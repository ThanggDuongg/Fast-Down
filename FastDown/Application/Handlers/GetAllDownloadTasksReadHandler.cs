using FastDown.Application.Queries;
using FastDown.Core;
using FastDown.Domain.ReadModels;
using FastDown.Infrastructure.Persistence.MongoDB;

namespace FastDown.Application.Handlers
{
    public class GetAllDownloadTasksReadHandler(DownloadTaskReadRepository repository)
        : IHandler<GetAllDownloadTasksReadQuery, List<DownloadTaskReadModel>>
    {
        public async Task<List<DownloadTaskReadModel>> HandleAsync(
            GetAllDownloadTasksReadQuery request,
            CancellationToken cancellationToken = default
        )
        {
            return await repository.GetAllAsync(cancellationToken);
        }
    }
}
