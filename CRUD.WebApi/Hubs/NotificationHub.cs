using Microsoft.AspNetCore.SignalR;

namespace CRUD.WebApi.Hubs;

/// <summary>
/// Хаб уведомлений.
/// </summary>
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;
    private readonly ApiMeters _metrics;

    public NotificationHub(ILogger<NotificationHub> logger, ApiMeters metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    // Эти методы вызывает клиент

    /// <summary>
    /// Логирует и добавляет сведения в телеметрию о полезности уведомления.
    /// </summary>
    /// <param name="notificationId">Id уведомления.</param>
    public async Task IsUsefulNotification(Guid notificationId)
    {
        // Достаём UserId
        var userId = Context.UserIdentifier;
        if (userId == null || !Guid.TryParse(userId, out _))
        {
            _logger.LogWarning("{id} является null или не удалось спарсить в Guid.", nameof(Context.UserIdentifier));
            return;
        }

        // Телеметрия
        _logger.LogInformation("Для пользователя \"{userId}\" уведомление \"{notificationId}\" оказалось полезным.", userId, notificationId);
        _metrics.UsefulNotification(notificationId);

        await Task.CompletedTask;
    }
}