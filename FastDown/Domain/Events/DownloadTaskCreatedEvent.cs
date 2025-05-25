namespace FastDown.Domain.Events
{
    public class DownloadTaskCreatedEvent
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

