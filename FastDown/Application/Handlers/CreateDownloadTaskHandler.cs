using FastDown.Application.Commands;
using FastDown.Core;
using FastDown.Domain.Entities;
using FastDown.Domain.Events;
using FastDown.Infrastructure.Persistence;

namespace FastDown.Application.Handlers
{
    public class CreateDownloadTaskHandler(FDContext context, IEventPublisher eventPublisher)
        : IHandler<CreateDownloadTaskCommand, int>
    {
        public async Task<int> HandleAsync(
            CreateDownloadTaskCommand request,
            CancellationToken cancellationToken = default
        )
        {
            if (string.IsNullOrWhiteSpace(request.Url))
            {
                throw new ArgumentException("URL cannot be empty");
            }

            var downloadTask = new DownloadTask
            {
                Url = request.Url,
                FileName = string.IsNullOrWhiteSpace(request.FileName)
                    ? Path.GetFileName(new Uri(request.Url).LocalPath)
                    : request.FileName,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
            };

            context.DownloadTasks.Add(downloadTask);
            await context.SaveChangesAsync(cancellationToken);

            await eventPublisher.PublishAsync(
                new DownloadTaskCreatedEvent
                {
                    Id = downloadTask.Id,
                    Url = downloadTask.Url,
                    FileName = downloadTask.FileName,
                    Status = downloadTask.Status,
                    CreatedAt = downloadTask.CreatedAt,
                },
                cancellationToken
            );

            return downloadTask.Id;
        }
    }
}
