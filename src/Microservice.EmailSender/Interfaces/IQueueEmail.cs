using Microservice.EmailSender.Models;
using System.Diagnostics.CodeAnalysis;

namespace Microservice.EmailSender.Interfaces;

/// <summary>
/// Сервис для работы с очередью писем.
/// </summary>
public interface IQueueEmail
{
    /// <summary>
    /// Добавляет письмо в очередь.
    /// </summary>
    /// <exception cref="ArgumentNullException">Если <paramref name="letter"/> <see langword="null"/>.</exception>
    /// <param name="letter">Письмо.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    Task EnqueueAsync(Letter letter, CancellationToken ct = default);

    /// <summary>
    /// Добавляет письмо в очередь.
    /// </summary>
    /// <exception cref="ArgumentNullException">Если <paramref name="letterBackground"/> <see langword="null"/>.</exception>
    /// <param name="letterBackground">Письмо.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    Task EnqueueAsync(LetterBackground letterBackground, CancellationToken ct = default);

    /// <summary>
    /// Достаёт все письма из очереди.
    /// </summary>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    IAsyncEnumerable<LetterBackground> DequeueAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Пытается достать письмо из очереди.
    /// </summary>
    /// <param name="letter">Письмо.</param>
    /// <returns><see langword="true"/>, если письмо успешно возвращено.</returns>
    bool TryDequeue([MaybeNullWhen(false)] out LetterBackground letter);
}