using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Publication;

/// <summary>
/// DTO-модель для создания публикации через пользовательский API.
/// </summary>
public class ClientApiCreatePublicationDto
{
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
    /// Постоянный или одноразовый API-ключ пользователя.
    /// </summary>
    [JsonPropertyName("apiKey")]
    public required string ApiKey { get; set; }
}