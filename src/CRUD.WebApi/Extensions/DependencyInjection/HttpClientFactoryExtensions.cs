using System.Net.Http.Headers;
using System.Text;

namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> расширения HTTP Client'ов.
/// </summary>
public static class HttpClientFactoryExtensions
{
    /// <summary>
    /// Добавляет <see cref="IHttpClientFactory"/> и настраивает клиентов через <see cref="HttpClientFactoryServiceCollectionExtensions.AddHttpClient(IServiceCollection, string)"/>.
    /// </summary>
    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        PayManagerOptions payManagerOptions = configuration.GetSection(PayManagerOptions.SectionName).Get<PayManagerOptions>()!;
        SmsSenderOptions smsSenderOptions = configuration.GetSection(SmsSenderOptions.SectionName).Get<SmsSenderOptions>()!;
        TelegramIntegrationOptions telegramIntegrationOptions = configuration.GetSection(TelegramIntegrationOptions.SectionName).Get<TelegramIntegrationOptions>()!;
        EmailSenderOptions emailSenderOptions = configuration.GetSection(EmailSenderOptions.SectionName).Get<EmailSenderOptions>()!;
        MetricsOptions metricsOptions = configuration.GetSection(MetricsOptions.SectionName).Get<MetricsOptions>()!;

        services.AddHttpClient();

        // PayManager
        services.AddHttpClient(HttpClientNames.PayManager, options =>
        {
            options.BaseAddress = new Uri(payManagerOptions.ServiceURL);

            // Авторизация для каждого запроса
            options.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{payManagerOptions.ShopId}:{payManagerOptions.ApiKey}")));
            // ASCII, потому что это стандарт для "Basic" (хотя сейчас уже на UTF-8 переходят по RFC) +мне достаточно символов ASCII, без всяких китайских символов, и кириллицы.
            // Т.е, если, например, в ShopId будет кириллица, то она будет кодироваться знаками "?".
            // Ну, и поддержку старых систем никто не отменял, а новые будут работать, т.к символы ASCII в кодировке UTF-8 в точности совпадают с их кодировкой в ​​ASCII
            options.DefaultRequestHeaders.Add("Idempotence-Key", Guid.NewGuid().ToString());
            options.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        })
            .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryAsync(3, retryNumber => TimeSpan.FromMilliseconds(600)));

        // SmsSender
        services.AddHttpClient(HttpClientNames.SmsSender, options =>
        {
            options.BaseAddress = new Uri(smsSenderOptions.ServiceURL);

            // Авторизация для каждого запроса
            options.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{smsSenderOptions.Email}:{smsSenderOptions.ApiKey}")));
        })
            .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryAsync(3, retryNumber => TimeSpan.FromMilliseconds(600)));

        // TelegramIntegration
        services.AddHttpClient(HttpClientNames.TelegramIntegration, options =>
        {
            options.BaseAddress = new Uri(telegramIntegrationOptions.ServiceURL);

            // Авторизация для каждого запроса
            options.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", telegramIntegrationOptions.ApiKey);
            options.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        })
            .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryAsync(3, retryNumber => TimeSpan.FromMilliseconds(600)));

        // EmailSender
        services.AddHttpClient(HttpClientNames.EmailSender, (serviceProvider, options) =>
        {
            options.BaseAddress = new Uri(emailSenderOptions.ServiceURL);

            // Авторизация для каждого запроса
            using var scope = serviceProvider.CreateScope(); // Не CreateAsyncScope, т.к делегат Action
            var grpcTokenManager = scope.ServiceProvider.GetRequiredService<IGrpcTokenManager>();
            var token = grpcTokenManager.GenerateAuthEmailSenderToken();
            options.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        })
            .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryAsync(3, retryNumber => TimeSpan.FromMilliseconds(600)));

        // Prometheus
        services.AddHttpClient(HttpClientNames.Prometheus, (serviceProvider, options) =>
        {
            options.BaseAddress = new Uri(metricsOptions.PrometheusURL);
        })
            .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryAsync(3, retryNumber => TimeSpan.FromMilliseconds(600)));

        // PollyWaitAndRetry
        // Неудачные (мои) запросы повторяются до трех раз с задержкой 600 мс между попытками
        services.AddHttpClient(HttpClientNames.PollyWaitAndRetry)
            .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryAsync(3, retryNumber => TimeSpan.FromMilliseconds(600))); // В счёт идут только 5XX, 408, System.Net.Http.HttpRequestException

        // PollyDynamic
        // Если исходящий (мой) запрос является запросом GET, применяется время ожидания 10 секунд. Для остальных методов время ожидания — 20 секунд
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
        var longTimeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(20));
        services.AddHttpClient(HttpClientNames.PollyDynamic)
            .AddPolicyHandler(httpRequestMessage => httpRequestMessage.Method == HttpMethod.Get ? timeoutPolicy : longTimeoutPolicy);

        return services;
    }
}