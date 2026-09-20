namespace CRUD.WebApi.Extensions;

/// <summary>
/// <see cref="IServiceCollection"/> расширения для настройки приложения.
/// </summary>
/// <remarks>
/// Используется в <c>Program.cs</c>.
/// </remarks>
public static class ProgramExtensions
{
    /// <summary>
    /// Регистрация OpenAPI и версионирование.
    /// </summary>
    public static IServiceCollection AddApiDocumentationAndVersioning(this IServiceCollection services)
    {
        services
            .AddEndpointsApiExplorer()
            .AddCustomOpenApi()
            .AddCustomApiVersioning();

        return services;
    }

    /// <summary>
    /// Инфраструктурные веб-сервисы, обработка ошибок, форматирование и локализация.
    /// </summary>
    public static IServiceCollection AddWebInfrastructureAndFormatting(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services
            .AddCustomExceptionHandlers(environment)
            .AddProblemDetailsAndConverters()
            .AddCustomLocalization()
            .AddDirectoryBrowser()
            .AddHttpContextAccessor();

        return services;
    }

    /// <summary>
    /// Безопасность, заголовки, авторизация, ограничения трафика и CORS.
    /// </summary>
    public static IServiceCollection AddSecurityAndTrafficPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddCustomForwardedHeaders(configuration)
            .AddCustomCors(configuration)
            .AddCustomAuthenticationAndAuthorization()
            .AddCustomDataProtection()
            .AddCustomRequestTimeouts()
            .AddCustomRateLimiter(configuration);

        return services;
    }

    /// <summary>
    /// Кэширование (OutputCache, HybridCache).
    /// </summary>
    public static IServiceCollection AddCachingAndOptimization(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddCustomOutputCache()
            .AddCustomHybridCache(configuration);

        return services;
    }

    /// <summary>
    /// Внешние и внутренние транспортные интеграции (HTTP, gRPC, SignalR).
    /// </summary>
    public static IServiceCollection AddMessagingAndCommunication(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services
            .AddHttpClients(configuration)
            .AddCustomSignalR()
            .AddGrpcClients(configuration, environment);

        return services;
    }

    /// <summary>
    /// Наблюдаемость, метрики и Health Checks.
    /// </summary>
    public static IServiceCollection AddObservabilityAndHealth(this IServiceCollection services)
    {
        services
            .AddCustomHealthChecks()
            .AddCustomOpenTelemetry();

        return services;
    }

    /// <summary>
    /// Бизнес-логика, валидация и доменные сервисы.
    /// </summary>
    public static IServiceCollection AddApplicationCore(this IServiceCollection services, ProgramOptions programOptions)
    {
        services
            .AddApplicationCoreValidators()
            .AddApplicationCoreServices(programOptions);

        return services;
    }
}