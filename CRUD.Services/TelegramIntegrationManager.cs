using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CRUD.Services;

/// <inheritdoc cref="ITelegramIntegrationManager"/>
public class TelegramIntegrationManager : ITelegramIntegrationManager
{
    private readonly string URL;
    private readonly string ApiKey;
    private readonly int TimeToLive;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TelegramIntegrationManager> _logger;

    public TelegramIntegrationManager(IOptions<TelegramIntegrationOptions> options, IHttpClientFactory httpClientFactory, ILogger<TelegramIntegrationManager> logger)
    {
        URL = options.Value.ServiceURL;
        ApiKey = options.Value.ApiKey;
        TimeToLive = options.Value.TimeToLive;

        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> SendVerificationCodeTelegramAsync(string phoneNumber, string code, CancellationToken ct = default)
    {
        // Создаём клиент
        var httpClient = _httpClientFactory.CreateClient(HttpClientNames.TelegramIntegration);

        // Принимает ли пользователь коды подтверждения
        var requestId = await IsTelegramAbilityAsync(phoneNumber, httpClient, ct);
        if (requestId == string.Empty)
            return false;

        // Создаём запрос
        var url = "sendVerificationMessage";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        // Авторизация уже настроена в фабрике IHttpClientFactory

        // Тело запроса
        var body = new
        {
            phone_number = "+" + phoneNumber,
            code, // Можно указать code_length, вместо code и Телеграм сам сгенерирует
            ttl = TimeToLive,
            request_id = requestId
        };
        var json = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, Application.Json);
        request.Content = json;

        // Отправляем запрос (код подтверждения)
        using var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Не удалось отправить код подтверждения: \"{code}\". {reasonPhrase}.", code, response.ReasonPhrase);
            return false;
        }

        // Читаем содержимое ответа
        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: ct);

        // Ответ с ошибкой
        var ok = jsonDocument.RootElement.GetProperty("ok").GetBoolean();
        if (ok == false)
        {
            var error = jsonDocument.RootElement.GetProperty("error").GetString();

            _logger.LogError("Не удалось отправить код подтверждения. Ошибка: {error}.", error);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Проверяет можно ли отправить пользователю код подтверждения.
    /// </summary>
    /// <remarks>
    /// Возвращает <с>request_id</с>, если пользователь может принять код подтверждения.
    /// </remarks>
    /// <param name="phoneNumber">Телефонный номер пользователя.</param>
    /// <param name="httpClient"><see cref="HttpClient"/> для повторного использования.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><see cref="string.Empty"/>, если пользователь не может принять код подтверждения.</returns>
    private async Task<string> IsTelegramAbilityAsync(string phoneNumber, HttpClient httpClient, CancellationToken ct = default)
    {
        // Создаём запрос
        var url = "checkSendAbility";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        // Авторизация уже настроена в фабрике IHttpClientFactory

        // Тело запроса
        var body = new
        {
            phone_number = "+" + phoneNumber
        };
        var json = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, Application.Json);
        request.Content = json;

        // Отправляем запрос
        using var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Не удалось отправить запрос на способность пользователя принимать коды подтверждения. Причина: {reasonPhrase}.", response.ReasonPhrase);
            return string.Empty;
        }

        // Читаем содержимое ответа
        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: ct);

        // Ответ с ошибкой
        var ok = jsonDocument.RootElement.GetProperty("ok").GetBoolean();
        if (ok == false)
        {
            var error = jsonDocument.RootElement.GetProperty("error").GetString();

            _logger.LogError("Не удалось отправить запрос на способность пользователя принимать коды подтверждения. Ошибка: {error}.", error);
            return string.Empty;
        }

        var requestId = jsonDocument.RootElement.GetProperty("result").GetProperty("request_id").GetString();
        return requestId ?? string.Empty;
    }

    public async Task<bool> CheckConnectionAsync(CancellationToken ct = default)
    {
        // Создаём клиент
        var httpClient = _httpClientFactory.CreateClient(HttpClientNames.TelegramIntegration);

        // Создаём запрос
        var url = "";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        // Авторизация уже настроена в фабрике IHttpClientFactory

        // Отправляем запрос
        using var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Не удалось отправить запрос на проверку подключения к Telegram серверу. Причина: {reasonPhrase}.", response.ReasonPhrase);
            return false;
        }

        // Читаем содержимое ответа
        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: ct);

        // На данный момент у Telegram нет метода для проверки подключения, значит будем проверять через успешную авторизацию
        // Ответ с ошибкой
        var ok = jsonDocument.RootElement.GetProperty("ok").GetBoolean();
        if (ok == false)
        {
            var error = jsonDocument.RootElement.GetProperty("error").GetString();
            if (error == "UNKNOWN_METHOD") // Неизвестный метод, значит авторизация прошла и подключение удалось
                return true;
            else if (error == "ACCESS_TOKEN_INVALID") // Авторизация не прошла
            {
                _logger.LogError("Не удалось отправить запрос на проверку подключения к Telegram серверу. Ошибка: {error} (не удалось авторизоваться).", error);
                return false;
            }
            else
            {
                _logger.LogError("Не удалось отправить запрос на проверку подключения к Telegram серверу. Ошибка: {error}.", error);
                return false;
            }
        }

        return false;
    }
}