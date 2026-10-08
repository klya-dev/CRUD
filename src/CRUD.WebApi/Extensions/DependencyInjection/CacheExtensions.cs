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

            // Тут тоже будут учитываться дефолтные правила
            options.AddPolicy("Expire20", builder =>
                builder.Expire(TimeSpan.FromSeconds(20)));
        });

        // Можно подключить Redis к OutputCache, я отключил, т.к это не HybridCache, и если нет подключения к редису, то локальный кэш не будет перехватывать управление (нет подключения - будет долго отвечать на запрос + исключение) | Лень запускать докер вместе с Redis'ом :)
        // Чтобы локальный кэш перехватывал управление при сбоях Redis нужно написать свою Fallback реализацию IOutputCacheStore. Можно без Fallback, просто написать реализацию, которая будет перехватывать исключения
        // Эта регистрация не дополняет дефолтный OutputCache (Memory), а заменяет (регистрирует новую реализацию) OutputCache на распределённое кэширование (Redis)
        //services.AddStackExchangeRedisOutputCache(options =>
        //{
        //    options.InstanceName = "localOutput";

        //    // Более гибкая настройка, чем "options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");"
        //    options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions()
        //    {
        //        EndPoints = new StackExchange.Redis.EndPointCollection()
        //        {
        //            { configuration.GetConnectionString("RedisConnection")! } // HostAndPort
        //        },
        //        ConnectRetry = 1, // Ограничиваем количество попыток
        //        ReconnectRetryPolicy = new StackExchange.Redis.ExponentialRetry(250), // Пауза между попытками
        //        AbortOnConnectFail = false, // Не выбрасывать исключения о таймауте
        //        ConnectTimeout = 250, // Лимит на подключение
        //        SyncTimeout = 250, // Лимит на выполнение синхронных команд (подключение уже установлено)
        //        AsyncTimeout = 250 // Лимит на асинхронных команд (подключение уже установлено)
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
    public static IServiceCollection AddCustomHybridCache(this IServiceCollection services)
    {
        services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = 1024 * 1024; // Максимальный размер записи кэша в байтах
            options.MaximumKeyLength = 1024; // Максимальная длина ключа в символах
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5), // Время истечения для Redis (Distributed)
                LocalCacheExpiration = TimeSpan.FromMinutes(5) // Время истечения для приложения (Memory)
            };
        });

        return services;
    }

    /// <summary>
    /// Добавляет и настраивает IDistributedCache (Redis).
    /// </summary>
    /// <remarks>
    /// Распределённое кэширование Redis.
    /// </remarks>
    public static IServiceCollection AddCustomDistributedCache(this IServiceCollection services, IConfiguration configuration)
    {
        // Регистрируем реализацию IDistributedCache (это не настройка к HybridCache, это самостоятельная реализация IDistributedCache. HybridCache сам берёт эту реализацию из IDistributedCache)
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