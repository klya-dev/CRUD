using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CRUD.Utility.Converters;

/// <summary>
/// <see cref="DateTime"/> конвертер, который меняет формат записи даты на <see cref="DateTimeFormats.Default"/> или указанный.
/// </summary>
public class DateTimeConverter : JsonConverter<DateTime>
{
    private readonly string _format;
    private readonly bool _useUniversalTime;

    /// <summary>
    /// Создаёт экземпляр <see cref="DateTimeConverter"/> с форматом по умолчанию.
    /// </summary>
    public DateTimeConverter() : this(DateTimeFormats.Default) { }

    /// <summary>
    /// Создаёт новый экземпляр <see cref="DateTimeConverter"/> с указанным форматом.
    /// </summary>
    /// <param name="format">Формат. Например, <c>"yyyy-MM-ddTHH:mm:ss.ffffffZ"</c> или из констант <see cref="DateTimeFormats"/>.</param>
    public DateTimeConverter(string format)
    {
        _format = format ?? throw new ArgumentNullException(nameof(format));
        _useUniversalTime = format?.Contains('Z') == true;
    }

    // Если атрибут DateTimeFormatJsonAttribute указан у свойства, то jsonDocument.Deserialize переводил время UTC в локальное (хотя по дефолту UTC должен остаться)
    // Поэтому пришлось исправлять эту медвежью услугу (при помощи reader.GetDateTime())

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetDateTime(); // В отличии от DateTime.Parse, дата не переводится в локальное время при использовании с DateTimeFormatJsonAttribute. И это правильно
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        DateTime dateToWrite = _useUniversalTime ?
            (value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime()) :
            value;

        string formattedDate = dateToWrite.ToString(_format, CultureInfo.InvariantCulture);
        writer.WriteStringValue(formattedDate);
    }
}