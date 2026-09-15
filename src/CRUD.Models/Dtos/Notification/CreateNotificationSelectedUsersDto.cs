using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Notification;

/// <summary>
/// DTO-модель создания уведомления для указанных пользователей из коллекции.
/// </summary>
public sealed record CreateNotificationSelectedUsersDto
{
    /// <summary>
    /// Коллекция Id пользователей.
    /// </summary>
    [JsonPropertyName("userIds")]
    public required IEnumerable<Guid> UserIds { get; init; }

    /// <summary>
    /// DTO-модель создания уведомления.
    /// </summary>
    [JsonPropertyName("notification")]
    public required CreateNotificationDto Notification { get; init; }
}