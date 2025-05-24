using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FastDown.API.Extensions
{
    public static class PropertyBuilderExtension
    {
        public static PropertyBuilder HasUnicodeTextColumn(
            this PropertyBuilder propertyBuilder,
            int? maxLength = null
        )
        {
            return maxLength.HasValue
                ? propertyBuilder.HasColumnType("nvarchar").HasMaxLength(maxLength.Value)
                : propertyBuilder.HasColumnType("nvarchar(max)");
        }

        public static PropertyBuilder HasAsciiColumn(
            this PropertyBuilder propertyBuilder,
            int? maxLength = null
        )
        {
            return maxLength.HasValue
                ? propertyBuilder.HasColumnType("varchar").HasMaxLength(maxLength.Value)
                : propertyBuilder.HasColumnType("varchar(max)");
        }

        public static PropertyBuilder<ICollection<TEnum>?> HasEnumCollectionConversion<TEnum>(
            this PropertyBuilder<ICollection<TEnum>?> builder
        )
            where TEnum : struct, Enum
        {
            var converter = new ValueConverter<ICollection<TEnum>?, string?>(
                v => v == null ? null : string.Join(",", v.Select(e => e.ToString())),
                v =>
                    string.IsNullOrWhiteSpace(v)
                        ? null
                        : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => Enum.Parse<TEnum>(s.Trim()))
                            .ToList()
            );

            return builder.HasConversion(converter);
        }

        public static PropertyBuilder HasEnumToStringConversation<TEnum>(
            this PropertyBuilder<TEnum> propertyBuilder
        )
            where TEnum : struct, Enum
        {
            return propertyBuilder.HasConversion(new EnumToStringConverter<TEnum>());
        }

        public static PropertyBuilder HasEnumToStringConversation<TEnum>(
            this PropertyBuilder<TEnum?> propertyBuilder
        )
            where TEnum : struct, Enum
        {
            return propertyBuilder.HasConversion(new EnumToStringConverter<TEnum>());
        }

        public static PropertyBuilder<T> HasComputedColumn<T>(
            this PropertyBuilder<T> propertyBuilder,
            string sqlExpression,
            bool stored = false
        )
        {
            if (string.IsNullOrWhiteSpace(sqlExpression))
            {
                throw new ArgumentException(
                    "SQL expression must not be null or empty",
                    nameof(sqlExpression)
                );
            }

            propertyBuilder.HasComputedColumnSql(sqlExpression);
            propertyBuilder.ValueGeneratedOnAddOrUpdate();
            propertyBuilder.Metadata.SetIsStored(stored);

            return propertyBuilder;
        }
    }
}
