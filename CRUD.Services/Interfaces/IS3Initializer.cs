namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для инициализации экосистемы S3.
/// </summary>
public interface IS3Initializer
{
    /// <summary>
    /// Инициализирует всю экосистему S3 для корректной работы приложения.
    /// </summary>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    Task InitializeAsync(CancellationToken ct = default);
}