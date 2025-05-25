using FastDown.Application.Queries;
using FastDown.Core;
using FastDown.Domain.Entities;
using FastDown.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FastDown.Application.Handlers
{
    public class GetDownloadTaskByIdHandler(FDContext context)
        : IHandler<GetDownloadTaskByIdQuery, DownloadTask?>
    {
        public async Task<DownloadTask?> HandleAsync(
            GetDownloadTaskByIdQuery request,
            CancellationToken cancellationToken = default
        )
        {
            return await context.DownloadTasks.FirstOrDefaultAsync(
                x => x.Id == request.Id,
                cancellationToken
            );
        }
    }
}
