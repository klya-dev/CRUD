namespace CRUD.Models.Domains;

/// <summary>
/// Domain модель продукта.
/// </summary>
public class Product
{
    /// <summary>
    /// Имя продукта.
    /// </summary>
    /// <remarks>
    /// Из констант <see cref="Products"/>.
    /// </remarks>
    public required string Name { get; set; }

    /// <summary>
    /// Цена продукта.
    /// </summary>
    public required decimal Price { get; set; }

    /// <summary>
    /// Заказы этого продукта.
    /// </summary>
    /// <remarks>
    /// Необходимо прогружать.
    /// </remarks>
    public ICollection<Order>? Orders { get; set; }

    /// <summary>
    /// Версия данных продукта, при каждом обновлении данных продукта, обновляется.
    /// </summary>
    /// <remarks>
    /// Используется для решения конфликтов параллельности.
    /// </remarks>
    public byte[]? RowVersion { get; set; }
}