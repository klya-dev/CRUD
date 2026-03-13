using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Notification;

/// <summary>
/// DTO-модель создания уведомления.
/// </summary>
public class CreateNotificationDto
{
    /// <summary>
    /// Заголовок уведомления.
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    /// <summary>
    /// Содержимое уведомления.
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; set; }
}