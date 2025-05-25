using FastDown.Core;
using FastDown.Domain.Entities;

namespace FastDown.Application.Queries
{
    public class GetDownloadTaskByIdQuery : IQuery<DownloadTask?>
    {
        public int Id { get; set; }
    }
}
