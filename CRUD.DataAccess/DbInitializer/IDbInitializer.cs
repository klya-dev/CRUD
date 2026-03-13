namespace CRUD.DataAccess.DbInitializer;

/// <summary>
/// Сервис для инициализации базы данных в актуальное состояние.
/// </summary>
public interface IDbInitializer
{
    /// <summary>
    /// Асинхронно применяет все миграции, которые были определены в сборке, но не были применены в базе данных.
    /// </summary>
    /// <remarks>
    /// Если базы данных не существует, то она создатся.
    /// </remarks>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    Task InitializeAsync(CancellationToken ct = default);
}