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
    /// <term>Если после изменений данных сущности <see cref="Product"/>, сущность окажется невалидна, изменения не последуют</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если возник конфликт параллельности</term>
    /// <description>исключение <see cref="DbUpdateConcurrencyException"/> | <see cref="DbUpdateException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если сущность <see cref="Product"/> окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    Task AddProductsToDbAsync(CancellationToken ct = default);
}