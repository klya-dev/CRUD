namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис реализации отзыва/удаления истёкших Refresh-токенов в фоне.
/// </summary>
public interface IRevokeExpiredRefreshTokensBackgroundCore
{
    /// <summary>
    /// Удаляет истёкшие Refresh-токены.
    /// </summary>
    /// <remarks>
    /// <para>Использовать в цикле, в качестве логики итерации.</para>
    /// </remarks>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    Task DoWorkAsync(CancellationToken ct);
}