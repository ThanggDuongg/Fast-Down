using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FastDown.Domain.ReadModels
{
    public class DownloadTaskReadModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        
        public int OriginalId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

