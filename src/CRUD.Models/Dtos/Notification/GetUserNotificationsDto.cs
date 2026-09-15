using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Notification;

/// <summary>
/// DTO-модель для получения уведомлений.
/// </summary>
public sealed record GetUserNotificationsDto
{
    /// <summary>
    /// Количество уведомлений.
    /// </summary>
    [JsonPropertyName("count")]
    public required int Count { get; init; }
}