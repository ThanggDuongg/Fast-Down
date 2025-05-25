namespace FastDown.Domain.Events
{
    public class DownloadTaskStatusChangedEvent
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}