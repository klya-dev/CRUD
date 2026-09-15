using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Publication;

/// <summary>
/// DTO-модель для создания публикации.
/// </summary>
public sealed record CreatePublicationDto
{
    /// <summary>
    /// Заголовок публикации.
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>
    /// Содержимое публикации.
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }
}