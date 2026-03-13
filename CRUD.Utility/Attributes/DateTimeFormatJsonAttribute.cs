using CRUD.Utility.Converters;
using System.Text.Json.Serialization;

namespace CRUD.Utility.Attributes;

/// <summary>
/// Атрибут для задания формата даты <see cref="DateTime"/> в JSON.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class DateTimeFormatJsonAttribute : JsonConverterAttribute
{
    private readonly string _format;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="DateTimeFormatJsonAttribute"/> с указанным форматом.
    /// </summary>
    /// <param name="format">Формат. Например, <c>"yyyy-MM-ddTHH:mm:ss.ffffffZ"</c>.</param>
    public DateTimeFormatJsonAttribute(string format) => _format = format;

    public override JsonConverter CreateConverter(Type typeToConvert)
    {
        if (typeToConvert != typeof(DateTime) && typeToConvert != typeof(DateTime?))
            throw new ArgumentException($"This converter only works with DateTime, and it was provided {typeToConvert.Name}.");

        return new DateTimeConverter(_format);
    }
}