using FastDown.API.Extensions;
using FastDown.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FastDown.Infrastructure.Persistence.EntityConfigs
{
    public class DownloadTaskEntityConfig : IEntityTypeConfiguration<DownloadTask>
    {
        public void Configure(EntityTypeBuilder<DownloadTask> builder)
        {
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
            builder.Property(x => x.Url).HasUnicodeTextColumn().IsRequired();
            builder.Property(x => x.FileName).HasUnicodeTextColumn(100).IsRequired();
            builder.Property(x => x.Status).HasUnicodeTextColumn(100).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
        }
    }
}
