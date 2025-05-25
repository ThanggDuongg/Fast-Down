namespace FastDown.Domain.Events
{
    public class DownloadTaskProgressEvent
    {
        public int Id { get; set; }
        public long BytesDownloaded { get; set; }
        public long TotalBytes { get; set; }
        public int ProgressPercentage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}