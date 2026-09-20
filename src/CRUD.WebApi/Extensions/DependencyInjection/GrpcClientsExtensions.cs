using Grpc.Net.Client.Configuration;
using System.Net.Sockets;

namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> расширения gRPC клиентов.
/// </summary>
public static class GrpcClientsExtensions
{
    /// <summary>
    /// Добавляет и настраивает gRPC клиенты.
    /// </summary>
    public static IServiceCollection AddGrpcClients(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var emailSenderOptions = configuration.GetSection(EmailSenderOptions.SectionName).Get<EmailSenderOptions>()!;

        // EmailSender
        services.AddGrpcClient<GrpcEmailSender.GrpcEmailSenderClient>(GrpcClientNames.GrpcEmailSender, options =>
        {
            options.Address = new Uri(emailSenderOptions.ServiceURL); // Url-адрес сервиса
        })
            .ConfigureChannel(options => // Настройка канала
            {
                // Политика повторов (https://learn.microsoft.com/ru-ru/aspnet/core/grpc/retries?view=aspnetcore-10.0)
                var defaultMethodConfig = new MethodConfig
                {
                    Names = { MethodName.Default },
                    RetryPolicy = new RetryPolicy
                    {
                        MaxAttempts = 3, // Максимальное количество попыток, считая первый запрос
                        InitialBackoff = TimeSpan.FromSeconds(1), // Начальная задержка между повторными попытками
                        MaxBackoff = TimeSpan.FromSeconds(3), // Верхний предел для увеличения экспоненциальной задержки (не больше 3 секунд задержка)
                        BackoffMultiplier = 1.5, // Множитель задержки (1с - 1.5с - 2.25с...) +это не точные числа, т.к внутри применяется рандом, чтобы не допустить объединения повторных попыток из нескольких вызовов в кластер и потенциальной перегрузки сервера
                        RetryableStatusCodes = { Grpc.Core.StatusCode.Unavailable } // Статус коды, на которые будет отрабатывать политика повторов
                    }
                };
                options.ServiceConfig = new ServiceConfig { MethodConfigs = { defaultMethodConfig } };
                options.MaxRetryAttempts = 3; // Что не было бы написано в конфиге выше, даём ограничение в 3 попытки +эта настройка только ограничивает, но не включает сам механизм повторов
                //options.LoggerFactory берётся из DI автоматически (https://learn.microsoft.com/en-us/aspnet/core/grpc/clientfactory?view=aspnetcore-10.0#configure-channel) +для Grpc.Net.Client.Internal.GrpcCall я указал минимальный уровень "Error"

                // Держать соединение открытым (keep-alive) (https://learn.microsoft.com/ru-ru/aspnet/core/grpc/performance?view=aspnetcore-10.0#keep-alive-pings)
                // +сервер микросервиса должен поддерживать keep-alive (options.Limits.KeepAliveTimeout)
                var handler = new SocketsHttpHandler
                {
                    PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan, // Как долго соединение может быть неактивным, чтобы можно было переиспользовать соединение
                    KeepAlivePingDelay = TimeSpan.FromSeconds(60), // Раз в 60 секунд пинговать (отправляет пакет keep-alive на сервер каждые 60 секунд в периоды бездействия)
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(30) // Если сервер в течении 30 секунд не ответит - разрываем соединение
                };

                // Взаимодействовать ли через Unix Domain Socket вместо TCP (в основном, если EmailSender и WebApi на одном ПК)
                if (emailSenderOptions.UseUnixDomainSocketGRPC)
                {
                    var udsEndPoint = new UnixDomainSocketEndPoint(Path.Combine(Path.GetTempPath(), emailSenderOptions.FileNameInTempFolder));
                    var connectionFactory = new UnixDomainSocketsConnectionFactory(udsEndPoint);
                    handler.ConnectCallback = connectionFactory.ConnectAsync;
                }

                options.HttpHandler = handler;

                // В разработке разрешаем использовать http (без TLS)
                if (environment.IsDevelopment())
                    options.UnsafeUseInsecureChannelCallCredentials = true;
            })
            .AddCallCredentials(async (context, metadata, serviceProvider) => // Аутентификация и авторизация
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var grpcTokenManager = scope.ServiceProvider.GetRequiredService<IGrpcTokenManager>();
                var token = grpcTokenManager.GenerateAuthEmailSenderToken();
                metadata.Add("Authorization", $"Bearer {token}");

                await Task.CompletedTask;
            });
            //.EnableCallContextPropagation(); // Передавать ct, deadline во внутрение вызовы сервисов АВТОМАТИЧЕСКИ (можно ручками через контекст) (https://learn.microsoft.com/ru-ru/aspnet/core/grpc/deadlines-cancellation?view=aspnetcore-10.0#propagating-deadlines)
            // У меня внутрених вызовов нет, у меня только один микросервис. Необходим нугет Grpc.AspNetCore.Server.ClientFactory

        return services;
    }
}