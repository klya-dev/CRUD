using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Reflection;

namespace CRUD.Test.Shared;

/// <summary>
/// Класс для работы с файлом <c>testsettings.json</c>.
/// </summary>
public class TestSettingsHelper
{
    // В свойствах testsettings.json указать, чтобы файл копировался в сборку, чтобы его можно было легче найти

    // Кэш для разных конфигураций. Нужно, чтобы при каждом вызове не перечитывать заново весь файл конфигурации
    // Например, "testsettings_json_only" - значит testsettings.json без учёта секретов
    // "$assembly.FullName" - значит testsettings.json + секреты пользователя из указанной сборки
    private static readonly ConcurrentDictionary<string, IConfiguration> _cache = new();

    /// <summary>
    /// Возвращает значение из тестовой конфигурации по указанному ключу.
    /// </summary>
    /// <remarks>
    /// <typeparamref name="TTarget"/> указывается, чтобы найти секреты пользователя в вызывающей сборке.
    /// </remarks>
    /// <param name="key">Ключ значения. Например, "Auth:Expires"</param>
    /// <returns>Значение из конфигурации типа <typeparamref name="T"/>.</returns>
    public static T? GetConfigurationValue<T, TTarget>(string key) where TTarget : class
    {
        var root = GetOrCreateConfiguration(typeof(TTarget).Assembly);
        var section = root.GetSection(key).Get<T>();

        return section;
    }

    /// <summary>
    /// Получает строку подключения к базе, которая находится в файле <c>testsettings.json</c>.
    /// </summary>
    /// <remarks>
    /// <typeparamref name="TTarget"/> указывается, чтобы найти секреты пользователя в вызывающей сборке.
    /// </remarks>
    /// <returns>Строка подключения.</returns>
    public static string GetDbConnectionString<TTarget>() where TTarget : class
    {
        var root = GetOrCreateConfiguration(typeof(TTarget).Assembly);
        var connectionString = root.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection (testsettings.json) является null");

        return connectionString;
    }

    /// <summary>
    /// Получает хост приложения, который находится в файле <c>testsettings.json</c>.
    /// </summary>
    /// <remarks>
    /// Например, "<c>localhost:7260</c>".
    /// </remarks>
    /// <returns>Хост приложения.</returns>
    public static string GetAppHost()
    {
        var root = GetOrCreateConfiguration(null);
        var host = root.GetValue<string>("AppHost") ?? throw new InvalidOperationException("AppHost (testsettings.json) является null");

        return host;
    }

    /// <summary>
    /// Получает или создаёт собранный <see cref="IConfiguration"/> в кэше.
    /// </summary>
    /// <remarks>
    /// <para>Нужно, чтобы при каждом вызове не перечитывать заново весь файл конфигурации.</para>
    /// <para>Если указана сборка <paramref name="assembly"/>, то к тестовой конфигурации ещё добавляются секреты пользователя из этой сборки.</para>
    /// </remarks>
    /// <param name="assembly">Сборка, в которой лежат секреты пользователя.</param>
    /// <returns>Собранная <see cref="IConfiguration"/>.</returns>
    private static IConfiguration GetOrCreateConfiguration(Assembly? assembly)
    {
        // Создаем уникальный ключ для кэша на основе имени сборки
        // Если сборка не указана, значит используем только testsettings.json без учёта секретов
        string cacheKey = assembly?.FullName ?? "testsettings_json_only";

        return _cache.GetOrAdd(cacheKey, _ =>
        {
            // Добавляем testsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("testsettings.json", optional: false);

            // Если указанна сборка, добавляем секреты из этой сборки
            if (assembly != null)
                builder.AddUserSecrets(assembly);

            return builder.Build();
        });
    }
}