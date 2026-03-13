using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Text.Json;

namespace CRUD.Tests.UnitTests;

public class OAuthMailRuProviderUnitTest
{
    private readonly OAuthMailRuProvider _oAuthMailRuProvider;
    private readonly Mock<IOptions<OAuthMailRuOptions>> _mockOptions;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<ILogger<OAuthMailRuProvider>> _mockLogger;
    private readonly Mock<IMemoryCache> _mockMemoryCache;

    public OAuthMailRuProviderUnitTest()
    {
        _mockOptions = new();
        _mockHttpClientFactory = new();
        _mockHttpMessageHandler = new();
        _mockLogger = new();
        _mockMemoryCache = new();

        _mockOptions.Setup(x => x.Value).Returns(new OAuthMailRuOptions() { ClientId = "", ClientSecret = "", RedirectUri = "", OpenIdConfigurationUrl = "" });

        // Мокаем создание клиента через фабрику
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        httpClient.BaseAddress = new Uri("https://localhost");
        _mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _oAuthMailRuProvider = new OAuthMailRuProvider(_mockOptions.Object, _mockHttpClientFactory.Object, _mockLogger.Object, _mockMemoryCache.Object);
    }

    [Fact]
    public async Task GetAuthorizationLinkAsync_ShouldReturnLink_WhenCorrectData()
    {
        // Arrange
        // Ответ OpenIdConfiguration
        var responseContent = new
        {
            issuer = "https://account.mail.ru",
            authorization_endpoint = "https://o2.mail.ru/login",
            token_endpoint = "https://o2.mail.ru/token",
            userinfo_endpoint = "https://o2.mail.ru/api/v1/oidc/userinfo",
            revocation_endpoint = "https://o2.mail.ru/api/v1/oauth2/token/revoke",
            response_types_supported = new string[] { "code", "token" },
            grant_types_supported = new string[] { "authorization_code", "autogen", "client_code", "client_credentials", "convert", "implicit", "password", "refresh_token", "urn:ietf:params:oauth:grant-type:token-exchange" },
            token_endpoint_auth_methods_supported = new string[] { "client_secret_post", "client_secret_basic" },
            introspection_endpoint = "https://o2.mail.ru/api/v1/oauth2/token/introspect"
        };
        var responseJson = JsonSerializer.Serialize(responseContent);
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        };

        // Ответ AuthorizationEndpoint
        var expectedUrl = "https:\\/\\/o2.mail.ru\\/xlogin?client_id=019c4c7a0a157d979cc3d28c8c5bd008&response_type=code&scope=&redirect_uri=https%3A%2F%2Flocalhost%3A7260&state=some_staten&code_challenge=7dSS0uu3KrSaWYtM0F2WZOiw_2yEqkGaKOmzcXIFm_E&code_challenge_method=S256";
        var responseContent2 = $"<script>var url = '{expectedUrl}';\r\n\t\t\t\tvar blocked =0;\r\n\t\t\t\tvar captcha =0;</script><script>\r\n\t\t\t\t(function (l, re, m) {{\r\n\t\t\t\t\tblocked && (url += '&blocked=1');\r\n\t\t\t\t\tcaptcha && (url += '&captcha=1');\r\n\r\n\t\t\t\t\twhile (m = re.exec(l)) {{\r\n\t\t\t\t\t\tif (url.indexOf(m[1]) === -1) {{\r\n\t\t\t\t\t\t\turl += '&' + m[1] + '=' + m[2];\r\n\t\t\t\t\t\t}}\r\n\t\t\t\t\t}}\r\n\r\n\t\t\t\t\tlocation.replace(url.replace('/login?', '/xlogin?'));\r\n\t\t\t\t}})(location.href, /&([^=]+)=([^&]+)/g, null);\r\n\t\t\t</script>";
        var responseMessage2 = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent2)
        };

        // Получение OpenIdConfiguration, AuthorizationEndpoint
        var queueStuff = new Queue<HttpResponseMessage>();
        queueStuff.Enqueue(responseMessage);
        queueStuff.Enqueue(responseMessage2);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(queueStuff.Dequeue);

        // Добавляем Verifier в кэш
        var verifier = "someverifier";
        var cacheEntry = Mock.Of<ICacheEntry>();
        Mock.Get(cacheEntry).SetupGet(c => c.Value).Returns(verifier);
        _mockMemoryCache.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(cacheEntry);

        // Act
        var result = await _oAuthMailRuProvider.GetAuthorizationLinkAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedUrl, result);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ShouldReturnAccessToken_WhenCorrectData()
    {
        // Arrange
        var code = "somecode";
        var state = "somestate";

        // Добавляем State и Verifier в словарь
        //var stateAndVerifierCodeDictionary = new Dictionary<string, string> { { state, code } };
        //var fieldInfo = typeof(OAuthMailRuProvider).GetField("_stateAndVerifierCodeDictionary", BindingFlags.NonPublic | BindingFlags.Instance);
        //fieldInfo.SetValue(_oAuthMailRuProvider, stateAndVerifierCodeDictionary);

        // Получаем Verifier из кэша
        var verifier = "someverifier";
        _mockMemoryCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny)) // Да, именно object (https://stackoverflow.com/a/79184819/31342728)
            .Callback((object key, out object value) =>
            {
                value = verifier;
            })
            .Returns(true);

        // Ответ OpenIdConfiguration
        var responseContent = new
        {
            issuer = "https://account.mail.ru",
            authorization_endpoint = "https://o2.mail.ru/login",
            token_endpoint = "https://o2.mail.ru/token",
            userinfo_endpoint = "https://o2.mail.ru/api/v1/oidc/userinfo",
            revocation_endpoint = "https://o2.mail.ru/api/v1/oauth2/token/revoke",
            response_types_supported = new string[] { "code", "token" },
            grant_types_supported = new string[] { "authorization_code", "autogen", "client_code", "client_credentials", "convert", "implicit", "password", "refresh_token", "urn:ietf:params:oauth:grant-type:token-exchange" },
            token_endpoint_auth_methods_supported = new string[] { "client_secret_post", "client_secret_basic" },
            introspection_endpoint = "https://o2.mail.ru/api/v1/oauth2/token/introspect"
        };
        var responseJson = JsonSerializer.Serialize(responseContent);
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        };

        // Ответ TokenEndpoint
        var expectedAccessToken = "32a86434e6ee445291d942615ce7ddf6a273c3b837363830";
        var responseContent2 = new
        {
            access_token = expectedAccessToken,
            token_type = "Bearer",
            expires_in = 3600
        };
        var responseJson2 = JsonSerializer.Serialize(responseContent2);
        var responseMessage2 = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson2)
        };

        // Получение OpenIdConfiguration, TokenEndpoint
        var queueStuff = new Queue<HttpResponseMessage>();
        queueStuff.Enqueue(responseMessage);
        queueStuff.Enqueue(responseMessage2);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(queueStuff.Dequeue);

        // Act
        var result = await _oAuthMailRuProvider.GetAccessTokenAsync(code, state);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedAccessToken, result);
    }

    [Fact]
    public async Task GetUserInfoAsync_ShouldReturnUserInfo_WhenCorrectData()
    {
        // Arrange
        var accessToken = "someaccesstoken";

        // Ответ OpenIdConfiguration
        var responseContent = new
        {
            issuer = "https://account.mail.ru",
            authorization_endpoint = "https://o2.mail.ru/login",
            token_endpoint = "https://o2.mail.ru/token",
            userinfo_endpoint = "https://o2.mail.ru/api/v1/oidc/userinfo",
            revocation_endpoint = "https://o2.mail.ru/api/v1/oauth2/token/revoke",
            response_types_supported = new string[] { "code", "token" },
            grant_types_supported = new string[] { "authorization_code", "autogen", "client_code", "client_credentials", "convert", "implicit", "password", "refresh_token", "urn:ietf:params:oauth:grant-type:token-exchange" },
            token_endpoint_auth_methods_supported = new string[] { "client_secret_post", "client_secret_basic" },
            introspection_endpoint = "https://o2.mail.ru/api/v1/oauth2/token/introspect"
        };
        var responseJson = JsonSerializer.Serialize(responseContent);
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        };

        // Ответ UserInfoEndpoint
        var expectedSub = "123";
        var responseContent2 = new
        {
            sub = expectedSub,
            name = "",
            given_name = "",
            family_name = "",
            nickname = "",
            picture = "",
            gender = "",
            birthdate = "2006-01-02",
            locale = "",
            email = "",
            email_verified = true,
        };
        var responseJson2 = JsonSerializer.Serialize(responseContent2);
        var responseMessage2 = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson2)
        };

        // Получение OpenIdConfiguration, UserInfoEndpoint
        var queueStuff = new Queue<HttpResponseMessage>();
        queueStuff.Enqueue(responseMessage);
        queueStuff.Enqueue(responseMessage2);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(queueStuff.Dequeue);

        // Act
        var result = await _oAuthMailRuProvider.GetUserInfoAsync(accessToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedSub, result.Sub);
    }
}