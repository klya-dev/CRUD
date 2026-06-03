namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для работы с продуктами.
/// </summary>
public interface IProductManager
{
    /// <summary>
    /// Добавляет все недостающие продукты в базу данных.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если возник конфликт параллельности</term>
    /// <description>исключение <see cref="DbUpdateConcurrencyException"/> | <see cref="DbUpdateException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    Task AddProductsToDbAsync(CancellationToken ct = default);
}