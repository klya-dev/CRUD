namespace CRUD.Models.Domains;

/// <summary>
/// Domain модель публикации.
/// </summary>
public class Publication
{
    /// <summary>
    /// Id публикации.
    /// </summary>
    /// <remarks>
    /// Генерируется при создании экземпляра, автоматически.
    /// </remarks>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Дата создания публикации.
    /// </summary>
    public required DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата изменения публикации.
    /// </summary>
    public DateTime? EditedAt { get; set; } = null;

    /// <summary>
    /// Заголовок публикации.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Содержимое публикации.
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Сущность автора (пользователя).
    /// </summary>
    /// <remarks>
    /// Необходимо прогружать по <see cref="AuthorId"/>.
    /// </remarks>
    public User? User { get; set; }

    /// <summary>
    /// Id автора (пользователя) публикации.
    /// </summary>
    public required Guid? AuthorId { get; set; }

    /// <summary>
    /// Версия данных публикации, при каждом обновлении данных публикации, обновляется.
    /// </summary>
    /// <remarks>
    /// Используется для решения конфликтов параллельности.
    /// </remarks>
    public byte[]? RowVersion { get; set; }
}