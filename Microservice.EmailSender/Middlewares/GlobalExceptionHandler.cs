namespace Microservice.EmailSender.Middlewares;

/// <summary>
/// Глобальный обработчик исключений.
/// </summary>
/// <remarks>
/// <para>Если исключение это <see cref="TimeoutException"/>, то возврат <see cref="StatusCodes.Status503ServiceUnavailable"/>.</para>    
/// <para>Если любое другое исключение, то возврат <see cref="StatusCodes.Status500InternalServerError"/>.</para>
/// <para>Создаётся ответ <see cref="ProblemDetails"/> со <see cref="ProblemDetails.Status"/> и <see cref="ProblemDetails.Title"/> без конфиденциальных данных.</para>
/// </remarks>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Произошло исключение: {Message}.", exception.Message);

        httpContext.Response.StatusCode = exception is TimeoutException // Если исключение это TimeoutException - 503 статус, иначе 500 статус
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status500InternalServerError;

        return new ValueTask<bool>(true); // Исключение успешно обработано, и не надо его обрабатывать ещё кем-то
    }
}