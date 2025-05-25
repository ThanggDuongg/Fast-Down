namespace FastDown.Infrastructure.Persistence.MongoDB
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string DownloadTasksCollectionName { get; set; } = string.Empty;
    }
}
