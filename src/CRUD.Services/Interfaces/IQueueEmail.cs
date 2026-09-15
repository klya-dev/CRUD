namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для работы с очередью писем.
/// </summary>
public interface IQueueEmail
{
    /// <summary>
    /// Добавляет письмо в очередь на отправку.
    /// </summary>
    /// <param name="letter">Электронное письмо.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="letter"/> <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="Grpc.Core.StatusCode"/>, статус код вызова gRPC.</returns>
    Task<Grpc.Core.StatusCode> EnqueueAsync(Letter letter, CancellationToken ct = default);
}