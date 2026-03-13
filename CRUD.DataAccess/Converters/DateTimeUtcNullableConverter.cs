using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CRUD.DataAccess.Converters;

/// <summary>
/// Конвертер для <see cref="Nullable{DateTime}"/>, преобразующий время в <c>UTC</c>.
/// </summary>
public class DateTimeUtcNullableConverter : ValueConverter<DateTime?, DateTime?>
{
    public DateTimeUtcNullableConverter() : base(
        d => d.HasValue ? d.Value.ToUniversalTime() : d,
        d => d.HasValue ? DateTime.SpecifyKind(d.Value, DateTimeKind.Utc) : d)
    { }
}