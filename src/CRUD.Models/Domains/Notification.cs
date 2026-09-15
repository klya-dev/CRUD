namespace CRUD.Models.Domains;

/// <summary>
/// Domain модель уведомления.
/// </summary>
public class Notification
{
    /// <summary>
    /// Id уведомления.
    /// </summary>
    /// <remarks>
    /// Генерируется при создании экземпляра, автоматически.
    /// </remarks>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Заголовок уведомления.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Содержимое уведомления.
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Дата создания уведомления.
    /// </summary>
    public required DateTime CreatedAt { get; set; }

    /// <summary>
    /// Версия данных уведомления, при каждом обновлении данных уведомления, обновляется.
    /// </summary>
    /// <remarks>
    /// Используется для решения конфликтов параллельности.
    /// </remarks>
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// Пользователи этого уведомления.
    /// </summary>
    /// <remarks>
    /// Необходимо прогружать.
    /// </remarks>
    public ICollection<User>? Users { get; set; }
}