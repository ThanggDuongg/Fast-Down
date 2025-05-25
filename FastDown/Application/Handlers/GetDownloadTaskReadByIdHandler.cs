using FastDown.Application.Queries;
using FastDown.Core;
using FastDown.Domain.ReadModels;
using FastDown.Infrastructure.Persistence.MongoDB;

namespace FastDown.Application.Handlers
{
    public class GetDownloadTaskReadByIdHandler(DownloadTaskReadRepository repository)
        : IHandler<GetDownloadTaskReadByIdQuery, DownloadTaskReadModel>
    {
        public async Task<DownloadTaskReadModel> HandleAsync(
            GetDownloadTaskReadByIdQuery request,
            CancellationToken cancellationToken = default
        )
        {
            return await repository.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}
