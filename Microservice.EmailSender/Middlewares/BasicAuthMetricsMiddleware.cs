using System.Text;

namespace Microservice.EmailSender.Middlewares;

/// <summary>
/// Middleware для проверки Basic авторизации у конечной точки "/metrics".
/// </summary>
/// <remarks>
/// Если авторизация не удалась, то добавляется заголовок <c>WWW-Authenticate</c>.
/// </remarks>
public class BasicAuthMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly MetricsOptions _options;

    public BasicAuthMetricsMiddleware(RequestDelegate next, IOptions<MetricsOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task Invoke(HttpContext context)
    {
        // Если конечная точка это "/metrics"
        if (context.Request.Path.StartsWithSegments("/metrics"))
        {
            // Получаем ожидаемые данные для авторизации из конфигурации
            string expectedUser = _options.User;
            string expectedPass = _options.Password;

            // Пытаемся получить Authorization заголовок
            if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                // Если это Basic авторизация
                var headerValue = authHeader.ToString();
                if (headerValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                {
                    // Декодируем строку из Base64 (формат "username:password")
                    var encodedCredentials = headerValue.Substring("Basic ".Length).Trim(); // Получаем закодированное содержимое Basic ("username:password")
                    var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials)).Split(':'); // Декодируем значения и сплитим в массив

                    // Логин, пароль совпадают
                    if (credentials.Length == 2 && credentials[0] == expectedUser && credentials[1] == expectedPass)
                    {
                        await _next(context);
                        return;
                    }
                }
            }

            // Если авторизация не удалась, добавляем заголовок
            context.Response.Headers.WWWAuthenticate = "Basic realm=\"Metrics EmailSender\"";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;

            return;
        }

        await _next(context);
    }
}