using FastDown.Domain.ReadModels;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace FastDown.Infrastructure.Persistence.MongoDB
{
    public class DownloadTaskReadRepository(
        IOptions<MongoDbSettings> settings,
        MongoDbContext context
    )
    {
        private readonly IMongoCollection<DownloadTaskReadModel> _collection =
            context.GetCollection<DownloadTaskReadModel>(
                settings.Value.DownloadTasksCollectionName
            );

        public async Task<List<DownloadTaskReadModel>> GetAllAsync(
            CancellationToken cancellationToken = default
        )
        {
            return await _collection
                .Find(_ => true)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<DownloadTaskReadModel> GetByIdAsync(
            int originalId,
            CancellationToken cancellationToken = default
        )
        {
            return await _collection
                .Find(x => x.OriginalId == originalId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<DownloadTaskReadModel>> GetByStatusAsync(
            string status,
            CancellationToken cancellationToken = default
        )
        {
            return await _collection
                .Find(x => x.Status == status)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task UpsertAsync(
            DownloadTaskReadModel task,
            CancellationToken cancellationToken = default
        )
        {
            var filter = Builders<DownloadTaskReadModel>.Filter.Eq(
                x => x.OriginalId,
                task.OriginalId
            );
            var options = new ReplaceOptions { IsUpsert = true };
            await _collection.ReplaceOneAsync(filter, task, options, cancellationToken);
        }
    }
}
