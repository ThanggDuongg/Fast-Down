using FastDown.Core;
using FastDown.Domain.ReadModels;

namespace FastDown.Application.Queries
{
    public class GetDownloadTaskReadByIdQuery : IQuery<DownloadTaskReadModel>
    {
        public int Id { get; set; }
    }
}
