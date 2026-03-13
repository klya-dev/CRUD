namespace CRUD.WebApi.HealthChecks;

/// <summary>
/// Проверяет подключение к OAuth MailRu серверу.
/// </summary>
public class OAuthMailRuConnectionHealthCheck : IHealthCheck
{
    private readonly IOAuthMailRuProvider _oAuthMailRuProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OAuthMailRuConnectionHealthCheck> _logger;

    public OAuthMailRuConnectionHealthCheck(IOAuthMailRuProvider oAuthMailRuProvider, IHttpClientFactory httpClientFactory, ILogger<OAuthMailRuConnectionHealthCheck> logger)
    {
        _oAuthMailRuProvider = oAuthMailRuProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Получаем конфигурацию
        var configuration = await _oAuthMailRuProvider.GetOpenIdConfiguration(cancellationToken);
        if (configuration == null)
            return HealthCheckResult.Unhealthy("Failed to get OpenIdConfiguration OAuth MailRu.");

        // Создание клиента
        var client = _httpClientFactory.CreateClient(HttpClientNames.PollyWaitAndRetry);
        client.Timeout = TimeSpan.FromMilliseconds(1000);

        // Создаём запрос
        var url = configuration.AuthorizationEndpoint;
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Отправляем запрос
        using var response = await client.SendAsync(request, cancellationToken);

        // Получаем BadRequest, значит ответил
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            _logger.LogError("Не удалось получить ответ от OAuth MailRu. Url: \"{url}\".", url);
            return HealthCheckResult.Unhealthy("Failed to connect to OAuth MailRu server.");
        }

        return HealthCheckResult.Healthy();
    }
}