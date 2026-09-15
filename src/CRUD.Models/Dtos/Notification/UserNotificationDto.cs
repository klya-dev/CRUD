using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Notification;

/// <summary>
/// DTO-модель уведомления пользователя.
/// </summary>
public sealed record UserNotificationDto
{
    /// <summary>
    /// Id уведомления.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

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

    /// <summary>
    /// Дата создания уведомления.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Прочитано ли уведомление пользователем.
    /// </summary>
    [JsonPropertyName("isRead")]
    public required bool IsRead { get; init; }
}