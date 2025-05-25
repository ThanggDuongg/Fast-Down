using FastDown.Core;

namespace FastDown.Application.Commands
{
    public class StartDownloadCommand : IRequest<bool>
    {
        public int DownloadTaskId { get; set; }
    }
}
