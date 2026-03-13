using System.Text.Json;
using System.Text.Json.Serialization;

namespace CRUD.Utility.Converters;

/// <summary>
/// <see cref="string"/> конвертер, который обрезает все лишние пробелы в начале и конце строки через метод <see cref="string.Trim()"/>.
/// </summary>
/// <remarks>
/// <para>Обрезка используется только для чтения (<see cref="string.Trim()"/>, есть только в <see cref="Read(ref Utf8JsonReader, Type, JsonSerializerOptions)"/> методе).</para>
/// <para>Кратко: Обрезает все входящие строки.</para>
/// </remarks>
public class TrimStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString()?.Trim();
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }

    // Желательно тоже переопределить, т.к без этого не работает DeveloperExceptionPage, и скорее всего что-то, где-то тоже отъебнёт
    public override string? ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString()?.Trim();
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value?.Trim() ?? "");
    }
}