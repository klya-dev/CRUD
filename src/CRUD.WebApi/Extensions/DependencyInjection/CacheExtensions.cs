using Microsoft.Extensions.Caching.Hybrid;

namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> расширения кэширования.
/// </summary>
public static class CacheExtensions
{
    /// <summary>
    /// Добавляет и настраивает Output Cache.
    /// </summary>
    /// <remarks>
    /// Кэширование HTTP ответов.
    /// </remarks>
    public static IServiceCollection AddCustomOutputCache(this IServiceCollection services)
    {
        services.AddOutputCache(options =>
        {
            // Политика по умолчанию. 200; GET, HEAD; запросы авторизованного пользователя не кэшируются; но время переопределенно | https://learn.microsoft.com/ru-ru/aspnet/core/performance/caching/output?view=aspnetcore-9.0#default-output-caching-policy
            options.AddBasePolicy(builder =>
                builder.Expire(TimeSpan.FromSeconds(10)));

            options.AddPolicy("Expire20", builder =>
                builder.Expire(TimeSpan.FromSeconds(20)));
        });

        // Можно подключить Redis к OutputCache, я отключил, т.к это не HybridCache, и если нет подключения к редису, то локальный кэш не будет перехватывать управление (будет долго отвечать на запрос)
        //builder.Services.AddStackExchangeRedisOutputCache(options =>
        //{
        //    options.InstanceName = "localOutput";

        //    // Более гибкая настройка, чем "options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");"
        //    options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions()
        //    {
        //        EndPoints = new StackExchange.Redis.EndPointCollection()
        //        {
        //            { builder.Configuration.GetConnectionString("RedisConnection")! } // HostAndPort
        //        },
        //        ConnectRetry = 0, // Ограничиваем количество попыток
        //        ReconnectRetryPolicy = new StackExchange.Redis.ExponentialRetry(250), // Пауза между попытками
        //        AbortOnConnectFail = false, // Не выбрасывать исключения о таймауте
        //        ConnectTimeout = 250, // Не больше 250 мс на подключение. Если, например, к редису не удалось подключиться во время запроса "/publications?count=1", то API ответит только через 250 мс + само подключение у Windows +- 2000 мс, т.к будет пытаться подключиться
        //        SyncTimeout = 250 // Работает в паре с ConnectTimeout, иначе не меняется. Хотя в RedisConnectionHealthCheck без него работает
        //    };
        //});

        return services;
    }

    /// <summary>
    /// Добавляет и настраивает Hybrid Cache.
    /// </summary>
    /// <remarks>
    /// Внутреннее кэширование приложения с Memory + Redis.
    /// </remarks>
    public static IServiceCollection AddCustomHybridCache(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = 1024 * 1024; // Максимальный размер кэша в байтах
            options.MaximumKeyLength = 1024; // Максимальная длина ключа в символах
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5), // Время истечения для Redis (Distributed)
                LocalCacheExpiration = TimeSpan.FromMinutes(5) // Время истечения для приложения (Memory)
            };
        });

        // Подключение Redis к HybridCache
        services.AddStackExchangeRedisCache(options =>
        {
            options.InstanceName = "localHybrid"; // Каждый ключ в кэше будет начинаться с этого префикса + полезно, если ферма приложений

            options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions()
            {
                EndPoints = new StackExchange.Redis.EndPointCollection()
                {
                    { configuration.GetConnectionString("RedisConnection")! } // HostAndPort
                },
                ConnectTimeout = 250,
                SyncTimeout = 250
            };
        });

        return services;
    }
}