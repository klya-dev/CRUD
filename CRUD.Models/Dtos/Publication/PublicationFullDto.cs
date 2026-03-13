using CRUD.Utility.Attributes;
using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Publication;

/// <summary>
/// DTO-модель полных данных публикации.
/// </summary>
public class PublicationFullDto
{
    /// <summary>
    /// Id публикации.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; set; }

    /// <summary>
    /// Дата создания публикации.
    /// </summary>
    [JsonPropertyName("createdAt")]
    [DateTimeFormatJson(DateTimeFormats.WithTicks)]
    public required DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата изменения публикации.
    /// </summary>
    [JsonPropertyName("editedAt")]
    [DateTimeFormatJson(DateTimeFormats.WithTicks)]
    public required DateTime? EditedAt { get; set; }

    /// <summary>
    /// Заголовок публикации.
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    /// <summary>
    /// Содержимое публикации.
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; set; }

    /// <summary>
    /// Id автора (пользователя) публикации.
    /// </summary>
    [JsonPropertyName("author")]
    public required UserFullDto? Author { get; set; }
}