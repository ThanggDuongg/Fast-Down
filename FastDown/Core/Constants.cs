namespace FastDown.Core
{
    public static class Constants
    {
        public static class HttpClients
        {
            public const string DownloadClient = "DownloadClient";
        }
        
        public static class DownloadTaskStatus
        {
            public const string Pending = "Pending";
            public const string InProgress = "InProgress";
            public const string Completed = "Completed";
            public const string Failed = "Failed";
        }
    }
}