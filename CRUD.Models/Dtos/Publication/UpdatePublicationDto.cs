using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Publication;

/// <summary>
/// DTO-модель для обновления данных публикации.
/// </summary>
public sealed record UpdatePublicationDto
{
    /// <summary>
    /// Id публикации.
    /// </summary>
    [JsonPropertyName("publicationId")]
    public required Guid PublicationId { get; init; }

    /// <summary>
    /// Новый заголовок публикации.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>
    /// Новое содержание публикации.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; init; }
}