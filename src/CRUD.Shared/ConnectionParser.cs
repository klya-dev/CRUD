using System.Diagnostics.CodeAnalysis;

namespace CRUD.Shared;

/// <summary>
/// Парсер строк подключения.
/// </summary>
public static class ConnectionParser
{
    /// <summary>
    /// Пытается создать <see cref="Uri"/> из строки подключения.
    /// </summary>
    /// <remarks>
    /// <para>Для строк такого вида: <c>localhost:5672</c> нужно указать <paramref name="defaultScheme"/>, иначе не получится создать <see cref="Uri"/>.</para>
    /// <para>Такая строка <c>amqp://admin:secret@rabbitmq.local:5672/vhost</c> без проблем создаст корректный результат <see cref="Uri"/> без указания схемы.</para>
    /// <para>Желательно всегда указывать схему, если она известна, ведь строки подключения могут смениться, а схема останется.</para>
    /// <para>Если порт не указан возвращается указанное значение по умолчанию.</para>
    /// </remarks>
    /// <param name="connectionString">Строка подключения.</param>
    /// <param name="uri"><see cref="Uri"/> результат.</param>
    /// <param name="defaultScheme">Схема по умолчанию. Если в строке подключения схема не указана, то будет подставляться это значение.</param>
    /// <param name="defaultPort">Порт по умолчанию. Если в строке подключения порт не указан, то будет подставляться это значение.</param>
    /// <returns><see langword="true"/>, если удалось создать <see cref="Uri"/>, иначе <see langword="false"/></returns>
    public static bool TryCreateUri(string connectionString, [NotNullWhen(true)] out Uri? uri, string defaultScheme = "http", int defaultPort = 8080)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(connectionString))
            return false;

        // Если строка не содержит "://", значит это неполный адрес (например, localhost:5672)
        // Добавляем к нему дефолтную схему
        string uriString = connectionString.Contains("://") ? connectionString : $"{defaultScheme}://{connectionString}";

        // Пытаемся пропарсить строку
        if (Uri.TryCreate(uriString, UriKind.Absolute, out uri))
        {
            // Если порт не указан (-1), вписываем дефолтное значение
            if (uri.Port < 0)
            {
                UriBuilder builder = new UriBuilder(uri);
                builder.Port = defaultPort;
                uri = builder.Uri;
            }

            return true;
        }

        return false;
    }
}