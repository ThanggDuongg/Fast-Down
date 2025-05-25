using FastDown.Core;
using FastDown.Domain.Events;
using FastDown.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FastDown.Infrastructure.Services
{
    public class DownloadService(
        HttpClient httpClient,
        FDContext context,
        IEventPublisher eventPublisher,
        ILogger<DownloadService> logger
    )
    {
        public async Task<bool> DownloadFileAsync(
            int downloadTaskId,
            CancellationToken cancellationToken = default
        )
        {
            var downloadTask = await context.DownloadTasks.FirstOrDefaultAsync(
                x => x.Id == downloadTaskId,
                cancellationToken
            );

            if (downloadTask == null)
            {
                logger.LogWarning("Download task with ID {Id} not found", downloadTaskId);
                return false;
            }

            try
            {
                var response = await httpClient.GetAsync(downloadTask.Url, cancellationToken);
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

                downloadTask.Status = "Completed";
                await context.SaveChangesAsync(cancellationToken);

                await eventPublisher.PublishAsync(
                    new DownloadTaskStatusChangedEvent
                    {
                        Id = downloadTask.Id,
                        Status = downloadTask.Status,
                    },
                    cancellationToken
                );

                logger.LogInformation(
                    "File downloaded successfully: {FileName}",
                    downloadTask.FileName
                );
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error downloading file from {Url}", downloadTask.Url);

                downloadTask.Status = "Failed";
                await context.SaveChangesAsync(cancellationToken);

                // Publish status changed event
                await eventPublisher.PublishAsync(
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
