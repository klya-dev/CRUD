using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;

namespace CRUD.Services;

/// <inheritdoc cref="ISmsSender"/>
public class SmsSender : ISmsSender
{
    private readonly string URL;
    private readonly string Email;
    private readonly string ApiKey;
    private readonly string Sign;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SmsSender> _logger;

    public SmsSender(IOptions<SmsSenderOptions> options, IHttpClientFactory httpClientFactory, ILogger<SmsSender> logger)
    {
        URL = options.Value.ServiceURL;
        Email = options.Value.Email;
        ApiKey = options.Value.ApiKey;
        Sign = options.Value.Sign;

        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> SendSmsAsync(string phoneNumber, string text, CancellationToken ct = default)
    {
        // Создаём клиент
        var httpClient = _httpClientFactory.CreateClient(HttpClientNames.SmsSender);

        // Создаём запрос
        var url = $"sms/send?number={HttpUtility.UrlEncode(phoneNumber)}&sign={HttpUtility.UrlEncode(Sign)}&text={HttpUtility.UrlEncode(text)}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        // Авторизация уже настроена в фабрике IHttpClientFactory

        // Отправляем запрос (СМС)
        using var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Не удалось отправить СМС: \"{text}\". {reasonPhrase}.", text, response.ReasonPhrase);
            return false;
        }

        return true;
    }

    public async Task<bool> TestAuthAsync(CancellationToken ct = default)
    {
        // Создаём клиент
        var httpClient = _httpClientFactory.CreateClient(HttpClientNames.SmsSender);

        // Создаём запрос
        var url = "auth";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        // Авторизация уже настроена в фабрике IHttpClientFactory

        // Отправляем запрос
        using var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Не удалось авторизоваться в сервисе отправки СМС: {reasonPhrase}.", response.ReasonPhrase);
            return false;
        }

        return true;
    }

    private async Task<string> GetSignAsync(CancellationToken ct = default)
    {
        var httpClient = _httpClientFactory.CreateClient(HttpClientNames.SmsSender);

        // Получить список имён
        var urlGetNames = "sign/list";
        var requestGetNames = new HttpRequestMessage(HttpMethod.Get, urlGetNames);
        // Авторизация уже настроена в фабрике IHttpClientFactory

        using var response = await httpClient.SendAsync(requestGetNames, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Не удалось получить список имён. {reasonPhrase}.", response.ReasonPhrase);
            return "";
        }

        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: ct);

        // Получение первого имени
        var firstname = jsonDocument.RootElement
            .GetProperty("data")
            .GetProperty("0")
            .GetProperty("name")
            .GetString() ?? string.Empty;

        return firstname;
    }
}