using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Publication;

/// <summary>
/// DTO-модель для получения авторов.
/// </summary>
public class GetAuthorsDto
{
    /// <summary>
    /// Количество авторов.
    /// </summary>
    [JsonPropertyName("count")]
    public required int Count { get; set; }
}