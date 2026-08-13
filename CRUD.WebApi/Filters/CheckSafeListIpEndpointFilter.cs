namespace CRUD.WebApi.Filters;

/// <summary>
/// Фильтр конечной точки сверяющий IP-адрес с указанным белым списком.
/// </summary>
/// <remarks>
/// Если IP-адреса нет в белом списке, то возвращается <see cref="StatusCodes.Status403Forbidden"/>.
/// </remarks>
public sealed class CheckSafeListIpEndpointFilter : IEndpointFilter
{
    private readonly List<IPNetwork> _networks;
    private readonly ILogger<CheckSafeListIpEndpointFilter> _logger;

    public CheckSafeListIpEndpointFilter(ILogger<CheckSafeListIpEndpointFilter> logger, IEnumerable<string> safeList)
    {
        _logger = logger;
        _networks = [];

        // Парсим CIDR'ы
        foreach (var cidr in safeList)
        {
            // Есть ли маска (символ '/')
            string networkString = cidr.Contains('/') ? cidr : $"{cidr}/32";

            if (IPNetwork.TryParse(networkString, out var network))
                _networks.Add(network);
            else
            {
                // Если это кривой IPv6 без маски
                if (!cidr.Contains('/') && cidr.Contains(':'))
                {
                    if (IPNetwork.TryParse($"{cidr}/128", out var networkV6))
                    {
                        _networks.Add(networkV6);
                        continue;
                    }
                }

                _logger.LogError("Не удалось пропарсить CIDR: \"{cidr}\".", cidr);
                throw new InvalidOperationException($"Not valid CIDR: {cidr}");
            }
        }
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Заголовки связанные с Forwarded из HttpContext.Request.Headers - Tuna (запрос от ЮКассы на вебхук)
        //*специально для этого запилил LoggingHeadersMiddleware

        // До app.UseForwardedHeaders(); (то что пришло с прокси в приложение)
        // Host: t9xxrp-217-114-0-69.ru.tuna.am
        // X-Forwarded-For: 77.75.153.78, 127.0.0.1
        // X-Forwarded-Host: t9xxrp-217-114-0-69.ru.tuna.am
        // X-Forwarded-Port: 443
        // X-Forwarded-Proto: https
        // X-Forwarded-Server: ru.tuna.am
        // X-Real-Ip: 77.75.153.78
        // X-Request-Id: 39FxKUSrARonREJIzBGWJUrsu5H

        // После app.UseForwardedHeaders(); (заголовки после обработки middleware)
        // *X-Forwarded-For заголовка нет, т.к значения сопоставились
        // Host: t9xxrp-217-114-0-69.ru.tuna.am
        // X-Forwarded-Host: t9xxrp-217-114-0-69.ru.tuna.am
        // X-Forwarded-Port: 443
        // X-Forwarded-Server: ru.tuna.am
        // X-Real-Ip: 77.75.153.78
        // X-Request-Id: 39FxKUSrARonREJIzBGWJUrsu5H
        // X-Original-For: 127.0.0.1:55062
        // X-Original-Proto: http

        // Тунели от Visual Studio не прописывают X-Forwarded заголовки, в отличии от Tuna | https://learn.microsoft.com/ru-ru/aspnet/core/test/dev-tunnels?view=aspnetcore-10.0

        var remoteIp = context.HttpContext.Connection.RemoteIpAddress;

        _logger.LogInformation("Remote IpAddress: {RemoteIp}.", remoteIp);

        if (remoteIp == null || !IsAllowed(remoteIp))
        {
            _logger.LogWarning("Forbidden Request from IP: {RemoteIp}.", remoteIp);
            return TypedResults.Forbid();
        }

        return await next(context);
    }

    /// <summary>
    /// Есть ли указанный IP-адрес в белом списке.
    /// </summary>
    private bool IsAllowed(IPAddress remoteIp)
    {
        // Приведение IPv4-mapped к IPv4 для корректного сравнения
        if (remoteIp.IsIPv4MappedToIPv6)
            remoteIp = remoteIp.MapToIPv4();

        foreach (var network in _networks)
        {
            if (network.Contains(remoteIp))
                return true;
        }

        return false;
    }
}