using FastDown.Application.Queries;
using FastDown.Core;
using FastDown.Domain.Entities;
using FastDown.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FastDown.Application.Handlers
{
    public class GetAllDownloadTasksHandler(FDContext context)
        : IHandler<GetAllDownloadTasksQuery, List<DownloadTask>>
    {
        public async Task<List<DownloadTask>> HandleAsync(
            GetAllDownloadTasksQuery request,
            CancellationToken cancellationToken = default
        )
        {
            return await context
                .DownloadTasks.OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }
}
