namespace CRUD.WebApi.Hubs;

/// <summary>
/// Статический класс с константами имён методов хаба.
/// </summary>
public static class HubMethodNames
{
    /// <summary>
    /// Получить уведомление.
    /// </summary>
    /// <remarks>
    /// Относится к <see cref="NotificationHub"/>.
    /// </remarks>
    public const string ReceiveNotification = "ReceiveNotification";
}