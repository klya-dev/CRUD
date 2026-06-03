using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Publication;

/// <summary>
/// DTO-модель для обновления полных данных публикации.
/// </summary>
public sealed record UpdatePublicationFullDto
{
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

    /// <summary>
    /// Новая дата в формате <see cref="DateTimeFormats.WithTicks"/>.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; init; }
}