using Grpc.Core;

namespace Microservice.EmailSender.Utilities;

/// <summary>
/// Расширения для <see cref="RpcException"/>.
/// </summary>
public static class RpcExceptionHelper
{
    /// <summary>
    /// Обработчик исключения.
    /// </summary>
    /// <remarks>
    /// Логирует и сопоставляет тип исключения, добавляет <c>CorrelationId</c> в <see cref="RpcException.Trailers"/>, заполняет <see cref="RpcException"/> (указывает <see cref="RpcException.Status"/>).
    /// </remarks>
    /// <typeparam name="T">Тип перехватчика (интерцептора).</typeparam>
    /// <param name="exception">Исключение.</param>
    /// <param name="context">Контекст.</param>
    /// <param name="logger">Логгер.</param>
    /// <param name="correlationId">Идентификатор корреляции.</param>
    /// <returns>Заполненый <see cref="RpcException"/>.</returns>
    public static RpcException Handle<T>(this Exception exception, ServerCallContext context, ILogger<T> logger, Guid correlationId) =>
        exception switch
        {
            TimeoutException => HandleTimeoutException((TimeoutException)exception, context, logger, correlationId),
            TaskCanceledException => HandleTaskCanceledException((TaskCanceledException)exception, context, logger, correlationId),
            //SqlException => HandleSqlException((SqlException)exception, context, logger, correlationId),
            RpcException => HandleRpcException((RpcException)exception, logger, correlationId),
            _ => HandleDefault(exception, context, logger, correlationId)
        };

    /// <summary>
    /// Обработчик <see cref="TimeoutException"/> исключений.
    /// </summary>
    /// <remarks>
    /// Логирует, добавляет трейлеры и заполняет <see cref="RpcException"/>.
    /// </remarks>
    /// <typeparam name="T">Тип перехватчика (интерцептора).</typeparam>
    /// <param name="exception">Исключение.</param>
    /// <param name="context">Контекст.</param>
    /// <param name="logger">Логгер.</param>
    /// <param name="correlationId">Идентификатор корреляции.</param>
    /// <returns>Заполненый <see cref="RpcException"/> со статусом <see cref="StatusCode.Internal"/>.</returns>
    private static RpcException HandleTimeoutException<T>(TimeoutException exception, ServerCallContext context, ILogger<T> logger, Guid correlationId)
    {
        logger.LogError(exception, "CorrelationId: {correlationId} - Произошло истечение времени", correlationId);

        var status = new Status(StatusCode.Internal, "An external resource did not answer within the time limit");
        return new RpcException(status, CreateTrailers(correlationId));
    }

    /// <summary>
    /// Обработчик <see cref="TaskCanceledException"/> исключений.
    /// </summary>
    /// <remarks>
    /// Логирует, добавляет трейлеры и заполняет <see cref="RpcException"/>.
    /// </remarks>
    /// <typeparam name="T">Тип перехватчика (интерцептора).</typeparam>
    /// <param name="exception">Исключение.</param>
    /// <param name="context">Контекст.</param>
    /// <param name="logger">Логгер.</param>
    /// <param name="correlationId">Идентификатор корреляции.</param>
    /// <returns>Заполненый <see cref="RpcException"/> со статусом <see cref="StatusCode.Cancelled"/>.</returns>
    private static RpcException HandleTaskCanceledException<T>(TaskCanceledException exception, ServerCallContext context, ILogger<T> logger, Guid correlationId)
    {
        logger.LogError(exception, "CorrelationId: {correlationId} - Произошла отмена", correlationId);

        var status = new Status(StatusCode.Cancelled, "The operation was canceled");
        return new RpcException(status, CreateTrailers(correlationId));
    }

    //private static RpcException HandleSqlException<T>(SqlException exception, ServerCallContext context, ILogger<T> logger, Guid correlationId)
    //{
    //    logger.LogError(exception, "CorrelationId: {correlationId} - An SQL error occurred", correlationId);
    //    Status status;

    //    if (exception.Number == -2)
    //        status = new Status(StatusCode.DeadlineExceeded, "SQL timeout");
    //    else
    //        status = new Status(StatusCode.Internal, "SQL error");
    //    return new RpcException(status, CreateTrailers(correlationId));
    //}

    /// <summary>
    /// Обработчик <see cref="RpcException"/> исключений.
    /// </summary>
    /// <remarks>
    /// Логирует, добавляет трейлеры и заполняет <see cref="RpcException"/>.
    /// </remarks>
    /// <typeparam name="T">Тип перехватчика (интерцептора).</typeparam>
    /// <param name="exception">Исключение.</param>
    /// <param name="logger">Логгер.</param>
    /// <param name="correlationId">Идентификатор корреляции.</param>
    /// <returns>Заполненый <see cref="RpcException"/> со статусом <see cref="RpcException.StatusCode"/>.</returns>
    private static RpcException HandleRpcException<T>(RpcException exception, ILogger<T> logger, Guid correlationId)
    {
        logger.LogError(exception, "CorrelationId: {correlationId} - Произошла ошибка", correlationId);

        // exception.Trailers после RpcException становится readonly и к нему добавить CorrelationId не получится, а вот создать новый Трейлер и перекинуть туда всё - можно
        // Делаю через метод CreateTrailers

        return new RpcException(new Status(exception.StatusCode, exception.Message), CreateTrailers(correlationId, exception.Trailers));
    }

    /// <summary>
    /// Обработчик по умолчанию.
    /// </summary>
    /// <remarks>
    /// <para>Логирует, добавляет трейлеры и заполняет <see cref="RpcException"/>.</para>
    /// <para>Используется для непредвиденных исключений.</para>
    /// </remarks>
    /// <typeparam name="T">Тип перехватчика (интерцептора).</typeparam>
    /// <param name="exception">Исключение.</param>
    /// <param name="context">Контекст.</param>
    /// <param name="logger">Логгер.</param>
    /// <param name="correlationId">Идентификатор корреляции.</param>
    /// <returns>Заполненый <see cref="RpcException"/> со статусом <see cref="StatusCode.Internal"/>.</returns>
    private static RpcException HandleDefault<T>(Exception exception, ServerCallContext context, ILogger<T> logger, Guid correlationId)
    {
        logger.LogError(exception, "CorrelationId: {correlationId} - Произошла непредвиденная ошибка", correlationId);

        // Не стоит передавать текст ошибки клиенту (exception.Message)
        return new RpcException(new Status(StatusCode.Internal, "Exception was thrown by handler"), CreateTrailers(correlationId));
    }

    /// <summary>
    /// Добавляет <c>CorrelationId</c> в трейлеры ответа.
    /// </summary>
    /// <param name="correlationId">Идентификатор корреляции.</param>
    /// <param name="trailersToAdd">Дополнительные метаданные.</param>
    /// <returns><see cref="Metadata"/> с <c>CorrelationId</c>.</returns>
    private static Metadata CreateTrailers(Guid correlationId, Metadata? trailersToAdd = null)
    {
        var trailers = new Metadata
        {
            { "correlationid", correlationId.ToString() } // Какой бы ключ не был он всё равно будет в нижнем регистре (поэтому лучше я явно укажу нижний)
        };

        // Если дополнительных метаданные не указаны
        if (trailersToAdd is null)
            return trailers;

        // Добавляем в новые метаданные дополнительные
        foreach (var trailer in trailersToAdd)
            trailers.Add(trailer);

        return trailers;
    }
}