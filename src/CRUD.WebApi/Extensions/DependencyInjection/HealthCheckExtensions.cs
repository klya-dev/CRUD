namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> расширения HealthCheck'ов.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Регистрирует HealthCheck'и.
    /// </summary>
    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseConnectionHealthCheck>(nameof(DatabaseConnectionHealthCheck)) // Проверка подключения к базе
            .AddCheck<DatabaseConsistencyHealthCheck>(nameof(DatabaseConsistencyHealthCheck)) // Проверка консистенции базы данных (существуют ли все таблицы)
            .AddCheck<S3ConnectionHealthCheck>(nameof(S3ConnectionHealthCheck)) // Проверка подключения к S3
            .AddCheck<S3ConsistencyHealthCheck>(nameof(S3ConsistencyHealthCheck)) // Проверка консистенции S3 (существуют ли все объекты)
            .AddCheck<RedisConnectionHealthCheck>(nameof(RedisConnectionHealthCheck)) // Проверка подключения к Redis серверу
            .AddCheck<EmailConnectionHealthCheck>(nameof(EmailConnectionHealthCheck)) // Проверка подключения к Email серверу
            .AddCheck<SmsConnectionHealthCheck>(nameof(SmsConnectionHealthCheck)) // Проверка подключения к СМС серверу
            .AddCheck<TelegramConnectionHealthCheck>(nameof(TelegramConnectionHealthCheck)) // Проверка подключения к Telegram
            .AddCheck<PaymentConnectionHealthCheck>(nameof(PaymentConnectionHealthCheck)) // Проверка подключения к платёжному серверу
            .AddCheck<PrometheusConnectionHealthCheck>(nameof(PrometheusConnectionHealthCheck)) // Проверка подключения к Prometheus серверу
            .AddCheck<HubsConnectionHealthCheck>(nameof(HubsConnectionHealthCheck)) // Проверка подключения к хабам
            .AddCheck<OAuthMailRuConnectionHealthCheck>(nameof(OAuthMailRuConnectionHealthCheck)) // Проверка подключения к OAuth MailRu
            .AddCheck<RabbitMqConnectionHealthCheck>(nameof(RabbitMqConnectionHealthCheck)); // Проверка подключения к RabbitMQ

        return services;
    }
}