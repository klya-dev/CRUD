namespace CRUD.WebApi.Middlewares;

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

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Произошло исключение: {Message}.", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = exception is TimeoutException // Если исключение это TimeoutException - 503 статус, иначе 500 статус
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status500InternalServerError,
            Title = exception is TimeoutException
                ? "Service Unavailable"
                : "Server Error"
        };
        // TimeoutException выбрасывается, если я выкинул, или например, какой-то сервис по типу Redis (но у него RedisConnectionException) 
        // TimeoutException не относится к ограничению времени для запросов, за это отвечает настройка builder.Services.AddRequestTimeouts, где падает 504 статус код
        // Тесты GlobalExceptionHandlerSystemTest, RequestTimeoutsSystemTest в помощь

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true; // Исключение успешно обработано, и не надо его обрабатывать ещё кем-то
    }
}