using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Publication;

/// <summary>
/// DTO-модель минимальных данных публикации.
/// </summary>
public sealed record PublicationDto
{
    /// <summary>
    /// Id публикации.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// Дата создания публикации.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Дата изменения публикации.
    /// </summary>
    [JsonPropertyName("editedAt")]
    public required DateTime? EditedAt { get; init; }

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

    /// <summary>
    /// Id автора (пользователя) публикации.
    /// </summary>
    [JsonPropertyName("authorId")]
    public required Guid? AuthorId { get; init; }

    /// <summary>
    /// Имя автора (пользователя) публикации.
    /// </summary>
    [JsonPropertyName("authorFirstname")]
    public required string AuthorFirstname { get; init; }
}