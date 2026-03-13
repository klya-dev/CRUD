using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Notification;

/// <summary>
/// DTO-модель уведомления пользователя.
/// </summary>
public class UserNotificationDto
{
    /// <summary>
    /// Id уведомления.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; set; }

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

    /// <summary>
    /// Дата создания уведомления.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public required DateTime CreatedAt { get; set; }

    /// <summary>
    /// Прочитано ли уведомление пользователем.
    /// </summary>
    [JsonPropertyName("isRead")]
    public required bool IsRead { get; set; }
}