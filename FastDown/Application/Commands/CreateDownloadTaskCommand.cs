using FastDown.Core;

namespace FastDown.Application.Commands
{
    public class CreateDownloadTaskCommand : ICommand<int>
    {
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}
