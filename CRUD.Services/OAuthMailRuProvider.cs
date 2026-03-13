using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CRUD.Services;

/// <inheritdoc cref="IOAuthMailRuProvider"/>
public partial class OAuthMailRuProvider : IOAuthMailRuProvider
{
    private readonly OAuthMailRuOptions _oAuthMailRuOptions;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OAuthMailRuProvider> _logger;
    private readonly IMemoryCache _cache; // Хочу хранить только в памяти, а не в Redis. HybridCache не подойдёт

    public OAuthMailRuProvider(IOptions<OAuthMailRuOptions> options, IHttpClientFactory httpClientFactory, ILogger<OAuthMailRuProvider> logger, IMemoryCache cache)
    {
        _oAuthMailRuOptions = options.Value;

        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _cache = cache;
    }

    public async Task<string?> GetAuthorizationLinkAsync(CancellationToken ct = default)
    {
        // Получаем OpenIdConfiguration
        var openIdConfiguration = await GetOpenIdConfiguration(ct);
        if (openIdConfiguration == null)
        {
            _logger.LogError("Не удалось получить OpenIdConfiguration.");
            return null;
        }

        // Создаём клиент
        var httpClient = _httpClientFactory.CreateClient(HttpClientNames.PollyWaitAndRetry);

        // Создаём запрос
        var scope = "openid%20email%profile"; // Запрашиваемые права приложения (openid, email, profile)
        var verifier = OAuthHelper.GenerateCodeVerifier(); // Случайная строка, которая используется для создания CodeChallenge
        var codeChallenge = OAuthHelper.GetCodeChallenge(verifier);
        var state = OAuthHelper.GenerateNonce(); // В конечной точке получения токена, по этому значению мы будем получать Verifier из кэша. Для защиты данных
        var endpoint = openIdConfiguration.AuthorizationEndpoint; // Получаем конечную точку

        var url = $"{endpoint}?response_type=code&client_id={_oAuthMailRuOptions.ClientId}&scope={scope}&redirect_uri={_oAuthMailRuOptions.RedirectUri}&state={state}&code_challenge={codeChallenge}&code_challenge_method=S256";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Отправляем запрос
        using var response = await httpClient.SendAsync(request, ct);

        // Неуспешный ответ
        if (!response.IsSuccessStatusCode)
        {
            await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
            using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: ct);

            _logger.LogError("Не удалось получить Url для авторизации в MailRu по причине: \"{error}\" ({description}).", jsonDocument.RootElement.GetProperty("error"), jsonDocument.RootElement.GetProperty("error_description"));
            return null;
        }

        // Читаем содержимое ответа
        var contentString = await response.Content.ReadAsStringAsync(ct);

        // Достаём Url из ответа
        var match = UrlFromScriptRegex().Match(contentString);
        var matchUrl = match.Groups[1].Value;
        if (matchUrl == string.Empty) // Если значения нет (string.Empty), возвращаем null
        {
            _logger.LogError("Не удалось достать Url для авторизации в MailRu из ответа. Ответ сервера: \"{content}\".", contentString);
            return null;
        }

        // Кэшируем State и CodeVerifier, чтобы получить AccessToken в другой конечной точке (кэшируем после успешного получения AccessToken, иначе нет смысла кэшировать)
        var options = new MemoryCacheEntryOptions
        {
            // 5 минут, потому что полученный, в случае успешной авторизации, код будет действовать только 5 минут (https://id.vk.com/about/business/go/docs/ru/vkid/latest/oauth/oauth-mail/index#Kak-poluchit-kod-avtorizacii)
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5), // Соотвественно, Verifier мне нужен будет только 5 минут
        };
        _cache.Set(state, verifier, options);

        return matchUrl;
    }

    public async Task<string?> GetAccessTokenAsync(string code, string state, CancellationToken ct = default)
    {
        // Если ли предоставленная строка состояния в кэше
        if (!_cache.TryGetValue(state, out string? verifierCode))
        {
            _logger.LogError("Не удалось получить AccessToken MailRu по причине: \"Предоставленная строка состояния не найдена в кэше\" (state: \"{state}\").", state);
            return null;
        }

        // Verifier Code null
        if (verifierCode == null)
        {
            _logger.LogError("Не удалось получить AccessToken MailRu по причине: \"Verifier Code успешно получен из кэша, но он null\" (state: \"{state}\").", state);
            return null;
        }

        // Получаем OpenIdConfiguration
        var openIdConfiguration = await GetOpenIdConfiguration(ct);
        if (openIdConfiguration == null)
        {
            _logger.LogError("Не удалось получить OpenIdConfiguration.");
            return null;
        }

        // Создаём клиент
        var httpClient = _httpClientFactory.CreateClient(HttpClientNames.PollyWaitAndRetry);

        // Создаём запрос
        var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_oAuthMailRuOptions.ClientId}:{_oAuthMailRuOptions.ClientSecret}"));
        var url = openIdConfiguration.TokenEndpoint;
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);

        // Тело запроса
        var body = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "code_verifier", verifierCode },
            { "redirect_uri", _oAuthMailRuOptions.RedirectUri }
        };

        // FormUrlEncodedContent автоматически установит Content-Type: application/x-www-form-urlencoded
        var content = new FormUrlEncodedContent(body);
        request.Content = content;

        // Отправляем запрос
        using var response = await httpClient.SendAsync(request, ct);

        // Читаем содержимое ответа
        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: ct);

        // Неуспешный ответ
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Не удалось получить AccessToken MailRu по причине: \"{error}\" ({description}).", jsonDocument.RootElement.GetProperty("error"), jsonDocument.RootElement.GetProperty("error_description"));
            return null;
        }

        // Достаём AccessToken
        if (!jsonDocument.RootElement.TryGetProperty("access_token", out JsonElement accessTokenElement)) // Пидоры на майле, возвращают 200 OK с полем "error", если что-то пошло не так
        {
            _logger.LogError("Не удалось получить AccessToken MailRu. Json: \"{response}\".", jsonDocument.RootElement);
            return null;
        }

        // Если AccessToken null
        var accessToken = accessTokenElement.GetString();
        if (accessToken == null)
        {
            _logger.LogError("Не удалось получить AccessToken MailRu по причине: \"\"access_token\" null\". Json: \"{response}\".", jsonDocument.RootElement);
            return null;
        }

        return accessToken;
    }

    public async Task<OpenIdUserInfo?> GetUserInfoAsync(string accessToken, CancellationToken ct = default)
    {
        // Получаем OpenIdConfiguration
        var openIdConfiguration = await GetOpenIdConfiguration(ct);
        if (openIdConfiguration == null)
        {
            _logger.LogError("Не удалось получить OpenIdConfiguration.");
            return null;
        }

        // Создаём клиент
        var httpClient = _httpClientFactory.CreateClient(HttpClientNames.PollyWaitAndRetry);

        // Создаём запрос
        var url = openIdConfiguration.UserInfoEndpoint;
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Отправляем запрос
        using var response = await httpClient.SendAsync(request, ct);

        // Читаем содержимое ответа
        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: ct);

        // Неуспешный ответ
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Не удалось получить UserInfo MailRu по причине: \"{error}\" ({description}).", jsonDocument.RootElement.GetProperty("error"), jsonDocument.RootElement.GetProperty("error_description"));
            return null;
        }

        // Парсим ответ в модель
        var openIdUserInfo = jsonDocument.Deserialize<OpenIdUserInfo>();
        if (openIdUserInfo == null)
        {
            _logger.LogError("Не удалось пропарсить ответ в модель. Json: \"{json}\".", jsonDocument.RootElement);
            return null;
        }

        return openIdUserInfo;
    }

    public async Task<OpenIdConfiguration?> GetOpenIdConfiguration(CancellationToken ct = default)
    {
        // Создаём клиент
        var httpClient = _httpClientFactory.CreateClient(HttpClientNames.PollyWaitAndRetry);

        // Создаём запрос
        var request = new HttpRequestMessage(HttpMethod.Get, _oAuthMailRuOptions.OpenIdConfigurationUrl);

        // Отправляем запрос
        using var response = await httpClient.SendAsync(request, ct);

        // Неуспешный ответ
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Не удалось получить OpenIdConfiguration. StatusCode: \"{code}\".", response.StatusCode);
            return null;
        }

        // Читаем содержимое ответа
        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: ct);

        // Парсим ответ в модель
        var openIdConfiguration = jsonDocument.Deserialize<OpenIdConfiguration>();
        if (openIdConfiguration == null)
        {
            _logger.LogError("Не удалось пропарсить ответ в модель. Json: \"{json}\".", jsonDocument.RootElement);
            return null;
        }

        return openIdConfiguration;
    }

    [GeneratedRegex("<script>var url = '(.*?)';")]
    private static partial Regex UrlFromScriptRegex(); // Достаёт Url из скрипта
}