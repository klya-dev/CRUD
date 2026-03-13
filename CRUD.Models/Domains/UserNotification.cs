namespace CRUD.Models.Domains;

/// <summary>
/// Связующая таблица пользователей с уведомлениями.
/// </summary>
public class UserNotification
{
    /// <summary>
    /// Id пользователя, к которому относится данное уведомление.
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Сущность пользователя.
    /// </summary>
    /// <remarks>
    /// Необходимо прогружать по <see cref="UserId"/>.
    /// </remarks>
    public User? User { get; set; }

    /// <summary>
    /// Id уведомления, к которому относится данный пользователь.
    /// </summary>
    public required Guid NotificationId { get; set; }

    /// <summary>
    /// Сущность уведомления.
    /// </summary>
    /// <remarks>
    /// Необходимо прогружать по <see cref="NotificationId"/>.
    /// </remarks>
    public Notification? Notification { get; set; }

    /// <summary>
    /// Прочитано ли уведомление пользователем.
    /// </summary>
    public bool IsRead { get; set; } = false;
}