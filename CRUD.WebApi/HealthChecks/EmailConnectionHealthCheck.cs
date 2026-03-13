using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace CRUD.WebApi.HealthChecks;

/// <summary>
/// Проверяет подключение к микросервису, отвечающему за отправку электронных писем.
/// </summary>
public class EmailConnectionHealthCheck : IHealthCheck
{
    private readonly string ServiceUrl;
    private readonly string HealthzEndpoint;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EmailConnectionHealthCheck> _logger;

    public EmailConnectionHealthCheck(IOptions<EmailSenderOptions> options, IHttpClientFactory httpClientFactory, ILogger<EmailConnectionHealthCheck> logger)
    {
        ServiceUrl = options.Value.ServiceURL;
        HealthzEndpoint = options.Value.HealthzEndpoint;

        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Создание клиента
        var client = _httpClientFactory.CreateClient(HttpClientNames.EmailSender);
        client.Timeout = TimeSpan.FromMilliseconds(1000);

        // Создаём запрос
        var url = HealthzEndpoint;
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        // Авторизация уже настроена в фабрике IHttpClientFactory

        // Отправляем запрос
        using var response = await client.SendAsync(request, cancellationToken);

        // Неуспешный ответ
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Не удалось подключится к микросервису, отвечающему за отправку электронных писем.");
            return HealthCheckResult.Unhealthy("Failed to connect to the microservice responsible for sending emails.");
        }

        return HealthCheckResult.Healthy();
    }
}