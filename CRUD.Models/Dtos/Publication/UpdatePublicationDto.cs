using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Publication;

/// <summary>
/// DTO-модель для обновления данных публикации.
/// </summary>
public class UpdatePublicationDto
{
    /// <summary>
    /// Id публикации.
    /// </summary>
    [JsonPropertyName("publicationId")]
    public required Guid PublicationId { get; set; }

    /// <summary>
    /// Новый заголовок публикации.
    /// </summary>
    /// <remarks>
    /// Необязательно.
    /// </remarks>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Новое содержание публикации.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}