namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис реализации удаления истёкших запросов (<see cref="Request"/>) в фоне.
/// </summary>
public interface IDeleteExpiredRequestsBackgroundCore
{
    /// <summary>
    /// Удаляет истёкшие запросы (<see cref="Request"/>).
    /// </summary>
    /// <remarks>
    /// <para>Использовать в цикле, в качестве логики итерации.</para>
    /// </remarks>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    Task DoWorkAsync(CancellationToken ct);
}