namespace FastDown.Domain.Entities
{
    public class DownloadTask
    {
        public int Id { get; set; }
        public string Url { get; set; } = default!;
        public string FileName { get; set; } = default!;
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public void MarkAsCompleted()
        {
            Status = "Completed";
        }

        public void MarkAsFailed()
        {
            Status = "Failed";
        }
    }
}
