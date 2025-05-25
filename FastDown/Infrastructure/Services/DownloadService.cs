using FastDown.Core;
using FastDown.Domain.Events;
using FastDown.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FastDown.Infrastructure.Services
{
    public class DownloadService
    {
        private readonly HttpClient _httpClient;
        private readonly FDContext _context;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<DownloadService> _logger;

        public DownloadService(
            HttpClient httpClient,
            FDContext context,
            IEventPublisher eventPublisher,
            ILogger<DownloadService> logger
        )
        {
            _httpClient = httpClient;
            _context = context;
            _eventPublisher = eventPublisher;
            _logger = logger;
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
                var response = await _httpClient.GetAsync(downloadTask.Url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var downloadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Downloads");
                if (!Directory.Exists(downloadsPath))
                {
                    Directory.CreateDirectory(downloadsPath);
                }

                var filePath = Path.Combine(downloadsPath, downloadTask.FileName);
                using (
                    var fileStream = new FileStream(
                        filePath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None
                    )
                )
                {
                    await response.Content.CopyToAsync(fileStream, cancellationToken);
                }

                var oldStatus = downloadTask.Status;
                downloadTask.Status = "Completed";
                await _context.SaveChangesAsync(cancellationToken);

                await _eventPublisher.PublishAsync(
                    new DownloadTaskStatusChangedEvent
                    {
                        Id = downloadTask.Id,
                        Status = downloadTask.Status,
                    },
                    cancellationToken
                );

                _logger.LogInformation(
                    "File downloaded successfully: {FileName}",
                    downloadTask.FileName
                );
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file from {Url}", downloadTask.Url);

                var oldStatus = downloadTask.Status;
                downloadTask.Status = "Failed";
                await _context.SaveChangesAsync(cancellationToken);

                // Publish status changed event
                await _eventPublisher.PublishAsync(
                    new DownloadTaskStatusChangedEvent
                    {
                        Id = downloadTask.Id,
                        Status = downloadTask.Status,
                    },
                    cancellationToken
                );

                return false;
            }
        }
    }
}
