namespace CRUD.Models.Domains;

/// <summary>
/// Domain модель запроса.
/// </summary>
public class Request
{
    /// <summary>
    /// Id запроса.
    /// </summary>
    /// <remarks>
    /// Генерируется при создании экземпляра, автоматически.
    /// </remarks>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Дата создания запроса.
    /// </summary>
    public required DateTime CreatedAt { get; set; }

    /// <summary>
    /// Срок истечения запроса.
    /// </summary>
    public required DateTime Expires { get; set; }

    /// <summary>
    /// Версия данных запроса, при каждом обновлении данных запроса, обновляется.
    /// </summary>
    /// <remarks>
    /// Используется для решения конфликтов параллельности.
    /// </remarks>
    public byte[]? RowVersion { get; set; }
}