using FastDown.Core;
using FastDown.Domain.ReadModels;

namespace FastDown.Application.Queries
{
    public class GetDownloadTasksByStatusQuery : IQuery<List<DownloadTaskReadModel>>
    {
        public string Status { get; set; } = string.Empty;
    }
}
