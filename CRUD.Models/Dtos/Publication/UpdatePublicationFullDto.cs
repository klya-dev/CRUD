using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Publication;

/// <summary>
/// DTO-модель для обновления полных данных публикации.
/// </summary>
public class UpdatePublicationFullDto
{
    /// <summary>
    /// Новый заголовок публикации.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Новое содержание публикации.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// Новая дата в формате <see cref="DateTimeFormats.WithTicks"/>.
    /// </summary>
    /// <remarks>
    /// Необязательно.
    /// </remarks>
    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }
}