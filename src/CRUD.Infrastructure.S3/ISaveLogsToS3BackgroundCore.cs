namespace CRUD.Infrastructure.S3;

/// <summary>
/// Сервис реализации логики сохранения файлов-логов в фоне.
/// </summary>
public interface ISaveLogsToS3BackgroundCore
{
    /// <summary>
    /// Сохраняет файлы-логи в облачное хранилище и удаляет локальные файлы-логи.
    /// </summary>
    /// <remarks>
    /// <para>Использовать в цикле, в качестве логики итерации.</para>
    /// <para>Реализованные функции:</para>
    /// <list type="bullet">
    /// <item>Сохранение файлов-логов в облачное хранилище.</item>
    /// <item>Удаление локальных файлов-логов.</item>
    /// <item>Метод не удалит сегодняшний лог.</item>
    /// </list>
    /// </remarks>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    Task DoWorkAsync(CancellationToken ct);
}