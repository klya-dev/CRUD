using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Notification;

/// <summary>
/// DTO-модель создания уведомления.
/// </summary>
public sealed record CreateNotificationDto
{
    /// <summary>
    /// Заголовок уведомления.
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>
    /// Содержимое уведомления.
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }
}