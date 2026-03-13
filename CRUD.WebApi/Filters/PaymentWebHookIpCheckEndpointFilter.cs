using Microsoft.Extensions.Options;
using System.Net.Sockets;

namespace CRUD.WebApi.Filters;

/// <summary>
/// Фильтр эндпоинта связанный с вебхук-оплатой.
/// </summary>
/// <remarks>
/// <para>Фильтр не пустит запрос, если <c>X-Forwarded-For</c> заголовка первого IP-адреса нет в списке разрешенных.</para>
/// <para>Список допустимых IP-адресов берётся из <see cref="PayManagerOptions"/>.</para>
/// </remarks>
public class PaymentWebHookIpCheckEndpointFilter : IEndpointFilter
{
    private readonly string _safelist;
    private readonly string[] _cidrs;
    private readonly ILogger<PaymentWebHookIpCheckEndpointFilter> _logger;

    public PaymentWebHookIpCheckEndpointFilter(IOptions<PayManagerOptions> options, ILogger<PaymentWebHookIpCheckEndpointFilter> logger)
    {
        _safelist = options.Value.SafeListIp;
        _safelist = options.Value.SafeListIp ?? string.Empty;
        _cidrs = _safelist.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        _logger = logger;
    }

    public virtual async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
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
        // Host: t9xxrp-217-114-0-69.ru.tuna.am
        // X-Forwarded-For: 77.75.153.78
        // X-Forwarded-Host: t9xxrp-217-114-0-69.ru.tuna.am
        // X-Forwarded-Port: 443
        // X-Forwarded-Server: ru.tuna.am
        // X-Real-Ip: 77.75.153.78
        // X-Request-Id: 39FxKUSrARonREJIzBGWJUrsu5H
        // X-Original-For: 127.0.0.1:55062
        // X-Original-Proto: http

        // IP с которого пришёл запрос (НО это может быть прокси, в моём случае приходило 127.0.0.1)
        var remoteIp = context.HttpContext.Connection.RemoteIpAddress;
        _logger.LogInformation("Remote IpAddress: {RemoteIp}.", remoteIp);

        // Тунели от Visual Studio не прописывают X-Forwarded заголовки, в отличии от Tuna. Поэтому, если использовать VS Tunnels, то закоментировать всё, что связанно с X-Forwarded ниже | https://learn.microsoft.com/ru-ru/aspnet/core/test/dev-tunnels?view=aspnetcore-10.0
        // По идеи в проде будет какой-нибудь nginx, который уже будет прописывать нужные заголовки

        // Вот тут уже реальное начальное IP (в Program.cs есть реализация всего этого)
        var xForwardedFor = context.HttpContext.Request.Headers["X-Forwarded-For"];
        _logger.LogInformation("X-Forwarded-For: {header}.", xForwardedFor);

        // Получаем начальное IP
        var firstValueXForwardedFor = xForwardedFor.FirstOrDefault();
        if (firstValueXForwardedFor == null)
        {
            _logger.LogWarning("First X-Forwarded-For is null.");
            return TypedResults.Forbid();
        }

        remoteIp = IPAddress.Parse(firstValueXForwardedFor);

        if (remoteIp == null)
        {
            _logger.LogWarning("Remote IP is null.");
            return TypedResults.Forbid();
        }

        // Если есть IPv4-mapped IPv6, привести к IPv4
        if (remoteIp.IsIPv4MappedToIPv6)
            remoteIp = remoteIp.MapToIPv4();

        if (!IsAllowed(remoteIp))
        {
            _logger.LogWarning("Forbidden Request from IP: {RemoteIp}.", remoteIp);
            return TypedResults.Forbid();
        }

        return await next(context);
    }

    // Преобразование строки в удобный объект проверки (CIDR parser)
    // Ниже — простая проверка для IPv4/IPv6 CIDR
    private static bool IsInCidr(IPAddress address, string cidr)
    {
        if (string.IsNullOrWhiteSpace(cidr)) return false;

        if (!cidr.Contains('/'))
        {
            if (!IPAddress.TryParse(cidr, out var single))
                return false;

            // Привести при необходимости
            if (single.AddressFamily == AddressFamily.InterNetwork && address.IsIPv4MappedToIPv6)
                address = address.MapToIPv4();
            if (single.AddressFamily == AddressFamily.InterNetworkV6 && address.AddressFamily == AddressFamily.InterNetwork)
                address = address.MapToIPv6();
            return single.Equals(address);
        }

        var parts = cidr.Split('/');
        if (parts.Length != 2)
            return false;

        if (!IPAddress.TryParse(parts[0], out var baseAddr))
            return false;

        if (!int.TryParse(parts[1], out int prefix))
            return false;

        // Приведение IPv4-mapped если нужно
        if (baseAddr.AddressFamily == AddressFamily.InterNetwork && address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();
        if (baseAddr.AddressFamily == AddressFamily.InterNetworkV6 && address.AddressFamily == AddressFamily.InterNetwork)
            address = address.MapToIPv6();

        var addrBytes = address.GetAddressBytes();
        var baseBytes = baseAddr.GetAddressBytes();
        if (addrBytes.Length != baseBytes.Length) return false;

        int bytesToCheck = prefix / 8;
        int bitsLeft = prefix % 8;
        for (int i = 0; i < bytesToCheck; i++)
            if (addrBytes[i] != baseBytes[i]) return false;
        if (bitsLeft > 0)
        {
            int mask = (byte)(~(0xFF >> bitsLeft));
            if ((addrBytes[bytesToCheck] & mask) != (baseBytes[bytesToCheck] & mask)) return false;
        }
        return true;
    }

    private bool IsAllowed(IPAddress remote)
    {
        foreach (var s in _cidrs)
            if (IsInCidr(remote, s))
                return true;

        return false;
    }
}