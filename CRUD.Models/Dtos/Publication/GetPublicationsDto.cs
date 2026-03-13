using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Publication;

/// <summary>
/// DTO-модель для получения публикаций.
/// </summary>
public class GetPublicationsDto
{
    /// <summary>
    /// Количество публикаций.
    /// </summary>
    [JsonPropertyName("count")]
    public required int Count { get; set; }
}