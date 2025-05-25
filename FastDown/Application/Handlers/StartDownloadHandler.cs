using FastDown.Application.Commands;
using FastDown.Core;
using FastDown.Infrastructure.Services;

namespace FastDown.Application.Handlers
{
    public class StartDownloadHandler(DownloadService downloadService)
        : IHandler<StartDownloadCommand, bool>
    {
        public async Task<bool> HandleAsync(
            StartDownloadCommand request,
            CancellationToken cancellationToken = default
        )
        {
            return await downloadService.DownloadFileAsync(
                request.DownloadTaskId,
                cancellationToken
            );
        }
    }
}
