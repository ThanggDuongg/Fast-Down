using Microsoft.EntityFrameworkCore;

namespace FastDown.API.Extensions
{
    public static class ModelBuilderExtension
    {
        public static void ConfigTableName(this ModelBuilder modelBuilder)
        {
            var clrTypes = GetClrTypes(modelBuilder);
            foreach (var clrType in clrTypes)
            {
                modelBuilder.Entity(clrType).ToTable(clrType.Name);
            }
        }

        private static Type[] GetClrTypes(ModelBuilder builder)
        {
            return
            [
                .. builder.Model.GetEntityTypes().Select(e => e.ClrType).Where(t => !t.IsAbstract),
            ];
        }
    }
}
