using System.Collections.Concurrent;
using System.Net.Http.Headers;
using FastDown.Core;
using FastDown.Domain.Events;
using FastDown.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;

namespace FastDown.Infrastructure.Services
{
    public class DownloadService
    {
        private readonly HttpClient _httpClient;
        private readonly FDContext _context;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<DownloadService> _logger;

        private const int DefaultChunkCount = 8;
        private const int BufferSize = 81920; // 80 KB

        private readonly ConcurrentDictionary<
            int,
            (long BytesDownloaded, long TotalBytes)
        > _progressTracker = new();

        private const int ProgressReportInterval = 5;
        private const int MaxRetries = 5;
        private readonly AsyncRetryPolicy _retryPolicy;

        public DownloadService(
            IHttpClientFactory httpClientFactory,
            FDContext context,
            IEventPublisher eventPublisher,
            ILogger<DownloadService> logger
        )
        {
            _httpClient = httpClientFactory.CreateClient(Constants.HttpClients.DownloadClient);
            _context = context;
            _eventPublisher = eventPublisher;
            _logger = logger;

            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .Or<IOException>()
                .Or<OperationCanceledException>()
                .WaitAndRetryAsync(
                    MaxRetries,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            exception,
                            "Error during HTTP request (attempt {RetryCount}/{MaxRetries}). Retrying in {RetryTimeSpan}...",
                            retryCount,
                            MaxRetries,
                            timeSpan
                        );
                    }
                );
        }

        public async Task<bool> DownloadFileAsync(
            int downloadTaskId,
            CancellationToken cancellationToken = default
        )
        {
            var downloadTask = await _context.DownloadTasks.FirstOrDefaultAsync(
                x => x.Id == downloadTaskId,
                cancellationToken
            );

            if (downloadTask == null)
            {
                _logger.LogWarning("Download task with ID {Id} not found", downloadTaskId);
                return false;
            }

            try
            {
                _progressTracker[downloadTaskId] = (0, 0);

                downloadTask.Status = Constants.DownloadTaskStatus.InProgress;
                await _context.SaveChangesAsync(cancellationToken);
                await PublishStatusChangedEvent(downloadTask, cancellationToken);

                var downloadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Downloads");
                if (!Directory.Exists(downloadsPath))
                {
                    Directory.CreateDirectory(downloadsPath);
                }

                var filePath = Path.Combine(downloadsPath, downloadTask.FileName);

                long fileSize = 0;
                bool supportsRanges = false;

                try
                {
                    (supportsRanges, fileSize) = await GetFileInfoAsync(
                        downloadTask.Url,
                        cancellationToken
                    );

                    _progressTracker[downloadTaskId] = (0, fileSize);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to get file info, proceeding with simple download"
                    );
                }

                if (!supportsRanges || fileSize < 1024 * 1024) // Less than 1MB
                {
                    await DownloadFileSimpleAsync(
                        downloadTask.Id,
                        downloadTask.Url,
                        filePath,
                        cancellationToken
                    );
                }
                else
                {
                    try
                    {
                        await DownloadFileInChunksAsync(
                            downloadTask.Id,
                            downloadTask.Url,
                            filePath,
                            fileSize,
                            DefaultChunkCount,
                            cancellationToken
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Chunked download failed, falling back to simple download"
                        );

                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }

                        await DownloadFileSimpleAsync(
                            downloadTask.Id,
                            downloadTask.Url,
                            filePath,
                            cancellationToken
                        );
                    }
                }

                downloadTask.Status = Constants.DownloadTaskStatus.Completed;
                await _context.SaveChangesAsync(cancellationToken);
                await PublishStatusChangedEvent(downloadTask, cancellationToken);

                _progressTracker.TryRemove(downloadTaskId, out _);

                _logger.LogInformation(
                    "File downloaded successfully: {FileName}",
                    downloadTask.FileName
                );
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file from {Url}", downloadTask.Url);

                downloadTask.Status = Constants.DownloadTaskStatus.Failed;
                await _context.SaveChangesAsync(cancellationToken);
                await PublishStatusChangedEvent(downloadTask, cancellationToken);

                _progressTracker.TryRemove(downloadTaskId, out _);

                return false;
            }
        }

        private async Task<(bool SupportsRanges, long FileSize)> GetFileInfoAsync(
            string url,
            CancellationToken cancellationToken
        )
        {
            try
            {
                using var getRequest = new HttpRequestMessage(HttpMethod.Get, url);
                using var getResponse = await _httpClient.SendAsync(
                    getRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken
                );

                getResponse.EnsureSuccessStatusCode();

                bool supportsRanges = getResponse.Headers.AcceptRanges.Contains("bytes");
                long fileSize = getResponse.Content.Headers.ContentLength ?? 0;

                if (fileSize > 0)
                {
                    _logger.LogInformation(
                        "File info from GET: Size={Size}, SupportsRanges={SupportsRanges}",
                        fileSize,
                        supportsRanges
                    );

                    return (supportsRanges, fileSize);
                }

                if (supportsRanges)
                {
                    using var rangeRequest = new HttpRequestMessage(HttpMethod.Get, url);
                    rangeRequest.Headers.Range = new RangeHeaderValue(0, 1); // Request just the first 2 bytes

                    using var rangeResponse = await _httpClient.SendAsync(
                        rangeRequest,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken
                    );

                    if (rangeResponse.StatusCode == System.Net.HttpStatusCode.PartialContent)
                    {
                        fileSize = rangeResponse.Content.Headers.ContentRange?.Length ?? 0;

                        _logger.LogInformation(
                            "File info from range request: Size={Size}, SupportsRanges=true",
                            fileSize
                        );

                        return (true, fileSize);
                    }
                }

                _logger.LogWarning(
                    "Could not determine file size. SupportsRanges={SupportsRanges}",
                    supportsRanges
                );

                return (supportsRanges, fileSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file info for {Url}", url);
                throw;
            }
        }

        private async Task DownloadFileSimpleAsync(
            int downloadTaskId,
            string url,
            string filePath,
            CancellationToken cancellationToken
        )
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                _logger.LogInformation("Starting simple download for {Url}", url);

                using var response = await _httpClient.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken
                );

                response.EnsureSuccessStatusCode();

                using var fileStream = new FileStream(
                    filePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    BufferSize,
                    true
                );

                using var downloadStream = await response.Content.ReadAsStreamAsync(
                    cancellationToken
                );

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                if (totalBytes > 0)
                {
                    _progressTracker[downloadTaskId] = (0, totalBytes);
                }

                var buffer = new byte[BufferSize];
                long bytesRead = 0;
                int read;
                int lastReportedProgress = 0;

                while (
                    (
                        read = await downloadStream.ReadAsync(
                            buffer,
                            0,
                            buffer.Length,
                            cancellationToken
                        )
                    ) > 0
                )
                {
                    await fileStream.WriteAsync(buffer, 0, read, cancellationToken);
                    bytesRead += read;

                    _progressTracker[downloadTaskId] = (bytesRead, totalBytes);

                    if (totalBytes > 0)
                    {
                        int progressPercentage = (int)((bytesRead * 100) / totalBytes);
                        if (progressPercentage >= lastReportedProgress + ProgressReportInterval)
                        {
                            lastReportedProgress = progressPercentage;
                            await PublishProgressEvent(
                                downloadTaskId,
                                bytesRead,
                                totalBytes,
                                progressPercentage,
                                cancellationToken
                            );
                        }
                    }
                }

                _logger.LogInformation("Simple download completed successfully for {Url}", url);
            });
        }

        private async Task DownloadFileInChunksAsync(
            int downloadTaskId,
            string url,
            string filePath,
            long fileSize,
            int chunkCount,
            CancellationToken cancellationToken
        )
        {
            var chunks = CalculateChunks(fileSize, chunkCount);
            _logger.LogInformation("Downloading file in {ChunkCount} chunks", chunks.Count);

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                fs.SetLength(fileSize);
            }

            var tasks = new List<Task>();
            var exceptions = new ConcurrentBag<Exception>();
            var completedChunks = new ConcurrentBag<(long Start, long End)>();
            var progressLock = new object();
            int lastReportedProgress = 0;

            int maxConcurrentChunks = Math.Min(
                4,
                Math.Min(Environment.ProcessorCount, chunks.Count)
            );

            using var semaphore = new SemaphoreSlim(maxConcurrentChunks);

            foreach (var chunk in chunks)
            {
                tasks.Add(
                    Task.Run(
                        async () =>
                        {
                            try
                            {
                                await semaphore.WaitAsync(cancellationToken);

                                try
                                {
                                    await _retryPolicy.ExecuteAsync(
                                        async () =>
                                            await DownloadChunkAsync(
                                                url,
                                                filePath,
                                                chunk.Start,
                                                chunk.End,
                                                cancellationToken
                                            )
                                    );

                                    completedChunks.Add(chunk);

                                    lock (progressLock)
                                    {
                                        long bytesDownloaded = completedChunks.Sum(c =>
                                            c.End - c.Start + 1
                                        );
                                        _progressTracker[downloadTaskId] = (
                                            bytesDownloaded,
                                            fileSize
                                        );

                                        int progressPercentage = (int)(
                                            (bytesDownloaded * 100) / fileSize
                                        );
                                        if (
                                            progressPercentage
                                            >= lastReportedProgress + ProgressReportInterval
                                        )
                                        {
                                            lastReportedProgress = progressPercentage;
                                            _ = PublishProgressEvent(
                                                downloadTaskId,
                                                bytesDownloaded,
                                                fileSize,
                                                progressPercentage,
                                                cancellationToken
                                            );
                                        }
                                    }
                                }
                                finally
                                {
                                    semaphore.Release();
                                }
                            }
                            catch (Exception ex)
                            {
                                exceptions.Add(ex);
                                _logger.LogError(
                                    ex,
                                    "Error downloading chunk {Start}-{End}",
                                    chunk.Start,
                                    chunk.End
                                );
                            }
                        },
                        cancellationToken
                    )
                );
            }

            await Task.WhenAll(tasks);

            if (!exceptions.IsEmpty)
            {
                throw new AggregateException("One or more chunks failed to download", exceptions);
            }
        }

        private async Task DownloadChunkAsync(
            string url,
            string filePath,
            long start,
            long end,
            CancellationToken cancellationToken
        )
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new RangeHeaderValue(start, end);

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            );

            response.EnsureSuccessStatusCode();

            using var downloadStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Write,
                FileShare.ReadWrite,
                BufferSize,
                true
            );

            fileStream.Seek(start, SeekOrigin.Begin);

            var buffer = new byte[BufferSize];
            int bytesRead;
            long totalBytesRead = 0;
            long expectedBytes = end - start + 1;

            while (
                (
                    bytesRead = await downloadStream.ReadAsync(
                        buffer,
                        0,
                        buffer.Length,
                        cancellationToken
                    )
                ) > 0
            )
            {
                int bytesToWrite = (int)Math.Min(bytesRead, expectedBytes - totalBytesRead);
                if (bytesToWrite <= 0)
                    break;

                await fileStream.WriteAsync(buffer, 0, bytesToWrite, cancellationToken);
                totalBytesRead += bytesToWrite;

                if (totalBytesRead >= expectedBytes)
                    break;
            }

            _logger.LogDebug(
                "Chunk {Start}-{End} downloaded successfully ({BytesRead} bytes)",
                start,
                end,
                totalBytesRead
            );

            if (totalBytesRead < expectedBytes)
            {
                _logger.LogWarning(
                    "Chunk {Start}-{End} incomplete: got {BytesRead} of {ExpectedBytes} bytes",
                    start,
                    end,
                    totalBytesRead,
                    expectedBytes
                );
            }
        }

        private static List<(long Start, long End)> CalculateChunks(long fileSize, int chunkCount)
        {
            var chunks = new List<(long Start, long End)>();

            int actualChunkCount = chunkCount;
            if (fileSize > 1024 * 1024 * 1024) // > 1GB
            {
                actualChunkCount = Math.Min(32, chunkCount * 2);
            }

            actualChunkCount = Math.Min(actualChunkCount, (int)(fileSize / (1024 * 1024)) + 1);
            actualChunkCount = Math.Max(1, actualChunkCount); // At least one chunk

            long chunkSize = fileSize / actualChunkCount;

            for (int i = 0; i < actualChunkCount; i++)
            {
                long start = i * chunkSize;
                long end = (i == actualChunkCount - 1) ? fileSize - 1 : ((i + 1) * chunkSize) - 1;

                chunks.Add((start, end));
            }

            return chunks;
        }

        private async Task PublishStatusChangedEvent(
            Domain.Entities.DownloadTask downloadTask,
            CancellationToken cancellationToken
        )
        {
            await _eventPublisher.PublishAsync(
                new DownloadTaskStatusChangedEvent
                {
                    Id = downloadTask.Id,
                    Status = downloadTask.Status,
                },
                cancellationToken
            );
        }

        private async Task PublishProgressEvent(
            int downloadTaskId,
            long bytesDownloaded,
            long totalBytes,
            int progressPercentage,
            CancellationToken cancellationToken
        )
        {
            await _eventPublisher.PublishAsync(
                new DownloadTaskProgressEvent
                {
                    Id = downloadTaskId,
                    BytesDownloaded = bytesDownloaded,
                    TotalBytes = totalBytes,
                    ProgressPercentage = progressPercentage,
                },
                cancellationToken
            );

            _logger.LogDebug(
                "Download progress for task {Id}: {Progress}% ({Downloaded}/{Total} bytes)",
                downloadTaskId,
                progressPercentage,
                bytesDownloaded,
                totalBytes
            );
        }
    }
}
