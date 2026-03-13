using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CRUD.DataAccess.Converters;

/// <summary>
/// Конвертер для <see cref="DateTime"/>, преобразующий время в <c>UTC</c>.
/// </summary>
public class DateTimeUtcConverter : ValueConverter<DateTime, DateTime>
{
    public DateTimeUtcConverter() : base(
        d => d.ToUniversalTime(), // В базу, всегда записывается UTC
        d => DateTime.SpecifyKind(d, DateTimeKind.Utc)) // Из базы достаётся UTC
    { }
}