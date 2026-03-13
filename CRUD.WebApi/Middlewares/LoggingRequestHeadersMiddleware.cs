namespace CRUD.WebApi.Middlewares;

/// <summary>
/// Middleware для логирования входящих заголовков HTTP-запросов.
/// </summary>
/// <remarks>
/// Существует нативный <see cref="Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware"/> (<c>UseHttpLogging</c>), но мне достаточно моего функционала.
/// </remarks>
public class LoggingRequestHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingRequestHeadersMiddleware> _logger;

    public LoggingRequestHeadersMiddleware(RequestDelegate next, ILogger<LoggingRequestHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Request.Headers;
        _logger.LogInformation("Request Headers: {headers}.", headers);
        _logger.LogInformation("Request Forwarded Headers: {headers}.", headers.Where(x => x.Key.StartsWith("X-")));

        await _next(context);
    }
}