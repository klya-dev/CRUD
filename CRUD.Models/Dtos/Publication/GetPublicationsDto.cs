using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Publication;

/// <summary>
/// DTO-модель для получения публикаций.
/// </summary>
public sealed record GetPublicationsDto
{
    /// <summary>
    /// Количество публикаций.
    /// </summary>
    [JsonPropertyName("count")]
    public required int Count { get; init; }
}