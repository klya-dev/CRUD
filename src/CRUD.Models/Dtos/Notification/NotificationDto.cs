using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Notification;

/// <summary>
/// DTO-модель уведомления.
/// </summary>
public sealed record NotificationDto
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
}