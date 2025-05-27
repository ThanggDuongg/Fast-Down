using FastDown.API.Extensions;
using FastDown.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FastDown.Infrastructure.Persistence
{
    public class FDContext(DbContextOptions<FDContext> options) : DbContext(options)
    {
        public DbSet<DownloadTask> DownloadTasks { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(FDContext).Assembly);
            modelBuilder.ConfigTableName();
        }
    }
}
