using FastDown.Domain.ReadModels;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
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
            try
            {
                var filter = Builders<DownloadTaskReadModel>.Filter.Eq(
                    x => x.OriginalId,
                    originalId
                );
                var result = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

                if (result == null)
                {
                    Console.WriteLine(
                        $"Document with OriginalId={originalId} not found in MongoDB"
                    );
                }

                return result!;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Error retrieving document with OriginalId={originalId}: {ex.Message}"
                );
                throw;
            }
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
            try
            {
                var filter = Builders<DownloadTaskReadModel>.Filter.Eq(
                    x => x.OriginalId,
                    task.OriginalId
                );

                var existingDocument = await _collection
                    .Find(filter)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingDocument != null)
                {
                    task.Id = existingDocument.Id ?? ObjectId.GenerateNewId().ToString();

                    await _collection.ReplaceOneAsync(
                        filter,
                        task,
                        new ReplaceOptions { IsUpsert = false },
                        cancellationToken
                    );
                }
                else
                {
                    if (string.IsNullOrEmpty(task.Id))
                    {
                        task.Id = ObjectId.GenerateNewId().ToString();
                    }

                    await _collection.InsertOneAsync(task, null, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Error upserting document with OriginalId={task.OriginalId}: {ex.Message}"
                );
                throw;
            }
        }
    }
}
