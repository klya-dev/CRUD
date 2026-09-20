using Microsoft.AspNetCore.Server.Kestrel;

namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="WebApplicationBuilder"/> расширения Kestrel.
/// </summary>
public static class KestrelExtensions
{
    /// <summary>
    /// Настраивает сервер (Kestrel).
    /// </summary>
    public static WebApplicationBuilder AddKestrel(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel((context, options) =>
        {
            // В целом, все значения по умолчанию меня устраивают, поэтому менять нечего

            // Можно донастроить конечные точки, т.к далеко не всё можно сделать через appsettings.json. Я нашёл такое применение, порт/протокол в appsettings, а более сложное уже тут, например, connection middleware (https://learn.microsoft.com/ru-ru/aspnet/core/fundamentals/servers/kestrel/connection-middleware?view=aspnetcore-10.0#create-custom-connection-middleware)
            // https://learn.microsoft.com/ru-ru/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0#configurationloader
            var kestrelSection = context.Configuration.GetSection("Kestrel");
            options.Configure(kestrelSection)
                .Endpoint("Https", (EndpointConfiguration endpointConfiguration) =>
                {
                    // ...
                })
                .Endpoint("Http", (EndpointConfiguration endpointConfiguration) =>
                {
                    // ...
                });

            options.Limits.MaxConcurrentConnections = 100; // Максимальное количество одновременных соединений
            options.Limits.MaxConcurrentUpgradedConnections = 100; // Максимальное количество обновлённых (соединение, которое было переключено с HTTP на другой протокол) соединений

            options.Limits.Http2.KeepAlivePingDelay = TimeSpan.FromSeconds(30); // Если сервер не получает никаких запросов от клиента в течение 30 секунд, он отправляет клиенту keep-alive пакет для проверки соединения (каждые 30 секунд неактивности отправляются пинги)
            options.Limits.Http2.KeepAlivePingTimeout = TimeSpan.FromMinutes(1); // Если клиент не отвечает на keep-alive или вообще ничего не отправляет в течении минуты - соединение разрывается (закрывает соединение, если в течении минуты не получен ответ)
        });

        builder.Services.Configure<HostOptions>(options =>
        {
            // Если возникло необработанное исключение в BackgroundService (ExecuteAsync), то по дефолту приложение падает
            // Даже, если исключение выбросилось не во время "поднятия" приложения, а вообще в любое время, например через час, приложение просто обрубается (при BackgroundServiceExceptionBehavior.StopHost - дефолт)
            options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore; // Игнорируем исключения (логирование остаётся) в фоновых сервисах и продолжаем работу приложения
        });

        return builder;
    }
}