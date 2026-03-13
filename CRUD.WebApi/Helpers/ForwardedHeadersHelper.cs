using System.Net;

namespace CRUD.WebApi.Helpers;

/// <summary>
/// Статический вспомогательный класс для работы с <c>Forwarded</c> заголовками.
/// </summary>
public static class ForwardedHeadersHelper
{
    /// <summary>
    /// Добавляет в список <see cref="ForwardedHeadersOptions.KnownNetworks"/> список IP-адресов через ';'.
    /// </summary>
    /// <remarks>
    /// Метод может использоваться, например, для добавления нескольких прокси-серверов.
    /// </remarks>
    /// <param name="safeList">Список IP-адресов через ';'.</param>
    /// <exception cref="InvalidOperationException">Если не удалось добавить IP-адрес.</exception>
    public static void AddKnownNetworks(this ForwardedHeadersOptions options, string safeList)
    {
        // Получаем значение из конфигурации
        var parts = safeList.Split([';'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var entryRaw in parts)
        {
            var entry = entryRaw.Trim();
            if (string.IsNullOrEmpty(entry))
                continue;

            // IPv6 CIDR содержит ":" и "/"
            if (entry.Contains('/'))
            {
                // Разделяем адрес и префикс
                var idx = entry.IndexOf('/');
                var ipPart = entry.Substring(0, idx);
                var prefixPart = entry.Substring(idx + 1);

                // Добавляем KnownNetworks
                if (IPAddress.TryParse(ipPart, out var networkIp) && int.TryParse(prefixPart, out var prefix))
                    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(networkIp, prefix));
                else
                    throw new InvalidOperationException($"Не удалось добавить \"{entry}\"");
            }
            else
            {
                // Одиночный IP
                if (IPAddress.TryParse(entry, out var singleIp))
                    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(singleIp, 0));
                else
                    throw new InvalidOperationException($"Не удалось добавить \"{entry}\"");
            }
        }
    }

    /// <summary>
    /// Возвращает первый IP-адрес из возможных по домену.
    /// </summary>
    /// <remarks>
    /// Домен указывается без "<c>http:// | https://</c>" (схемы).
    /// </remarks>
    /// <param name="domain">Домен. (без <c>http:// | https://</c>)</param>
    /// <returns>Первый IP-адрес домена.</returns>
    public static IPAddress GetIpByDomain(string domain)
    {
        var addresses = Dns.GetHostAddresses(domain);

        return addresses[0];
    }
}