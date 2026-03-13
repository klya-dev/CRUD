using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Notification;

/// <summary>
/// DTO-модель для получения уведомлений.
/// </summary>
public class GetUserNotificationsDto
{
    /// <summary>
    /// Количество уведомлений.
    /// </summary>
    [JsonPropertyName("count")]
    public required int Count { get; set; }
}