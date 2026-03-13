using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Publication;

/// <summary>
/// DTO-модель минимальных данных публикации.
/// </summary>
public class PublicationDto
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
    public required DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата изменения публикации.
    /// </summary>
    [JsonPropertyName("editedAt")]
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
    [JsonPropertyName("authorId")]
    public required Guid? AuthorId { get; set; }

    /// <summary>
    /// Имя автора (пользователя) публикации.
    /// </summary>
    [JsonPropertyName("authorFirstname")]
    public required string AuthorFirstname { get; set; }
}