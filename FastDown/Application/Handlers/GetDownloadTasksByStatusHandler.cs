using FastDown.Application.Queries;
using FastDown.Core;
using FastDown.Domain.ReadModels;
using FastDown.Infrastructure.Persistence.MongoDB;

namespace FastDown.Application.Handlers
{
    public class GetDownloadTasksByStatusHandler(DownloadTaskReadRepository repository)
        : IHandler<GetDownloadTasksByStatusQuery, List<DownloadTaskReadModel>>
    {
        public async Task<List<DownloadTaskReadModel>> HandleAsync(
            GetDownloadTasksByStatusQuery request,
            CancellationToken cancellationToken = default
        )
        {
            return await repository.GetByStatusAsync(request.Status, cancellationToken);
        }
    }
}
