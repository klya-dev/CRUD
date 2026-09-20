using Microsoft.AspNetCore.HttpOverrides;

namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> расширения Forwarded заголовков.
/// </summary>
public static class ForwardedHeaderExtensions
{
    /// <summary>
    /// Настраивает <see cref="ForwardedHeadersOptions"/>.
    /// </summary>
    public static IServiceCollection AddCustomForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // https://learn.microsoft.com/ru-ru/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-10.0&tabs=linux-ubuntu#use-a-reverse-proxy-server
            // Если прокси-сервер на одной машине с приложением, то адрес, считается доверенным (т.к 127.0.0.1) и его не нужно указывать в KnownProxies или KnownNetworks
            // Если прокси-сервер находится удалённо, то обязательно нужно указать KnownProxies (если IP-адрес один) или KnownNetworks (если IP-адресов несколько, например, целый кластер)

            // Поддержка удалённых прокси серверов
            ProxiesOptions proxiesOptions = configuration.GetSection(ProxiesOptions.SectionName).Get<ProxiesOptions>()!;

            foreach (var proxyIp in proxiesOptions.RemoteProxyIps)
            {
                // Диапазон или IP
                if (proxyIp.Contains('/'))
                {
                    if (System.Net.IPNetwork.TryParse(proxyIp, out var network))
                        options.KnownIPNetworks.Add(network);
                    else
                        throw new InvalidOperationException($"Not valid network from \"{ProxiesOptions.SectionName}:{nameof(ProxiesOptions.RemoteProxyIps)}\": \"{proxyIp}\".");
                }
                else if (IPAddress.TryParse(proxyIp, out var ipAddress))
                    options.KnownProxies.Add(ipAddress);
                else
                    throw new InvalidOperationException($"Not valid IP from \"{ProxiesOptions.SectionName}:{nameof(ProxiesOptions.RemoteProxyIps)}\": \"{proxyIp}\".");
            }

            options.ForwardLimit = proxiesOptions.ForwardLimit; // Позволяет обрабатывать цепочку X-Forwarded-For не более чем из двух элементов
            // В моём случае X-Forwarded-For: 77.75.153.78, 127.0.0.1
            // Где 77.75.153.78 - это IP-адрес платёжного шлюза (реальный отправитель запроса)
            // А 127.0.0.1 - это локальный IP-адрес моего прокси Tuna
            // ВАЖНО: если оставить по дефолту 1, то сопоставление с HttpContext.Connection.RemoteIpAddress не будет работать, т.к ASP.NET не посчитает значения из X-Forwarded-For доверенными IP-адресами
            // Т.е вместо 77.75.153.78, будет 127.0.0.1, что неверно

            // Т.к адрес прокси локальный добавлять его в KnownProxies не нужно
            // Хоть у Tuna есть домен, его IP-адрес не учавствует в цепочке X-Forwarded-For, поэтому добавлять его в KnownProxies не нужно
            // По идее nginx должен тоже корректно отрабатывать
        });

        return services;
    }
}