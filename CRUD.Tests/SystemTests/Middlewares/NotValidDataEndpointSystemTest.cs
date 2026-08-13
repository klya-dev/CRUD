using Microsoft.AspNetCore.Hosting;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CRUD.Tests.SystemTests.Middlewares;

[Collection(nameof(IntegrationsTestCollection))]
public sealed class NotValidDataEndpointSystemTest : IClassFixture<TestWebApplicationFactory>
{
    // Тут я тестирую невалидные данные и авторизацию

    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly ITokenManager _tokenManager;

    public NotValidDataEndpointSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        // Пересоздаю базу в NotValidBeforeUpdate
        _client = factory.WithWebHostBuilder(configuration =>
        {
            configuration.UseEnvironment("Production");
        }).CreateClient(); // Т.к Production может чуть иначе обрабатывать исключительные ситуации

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
    }

    [Theory]
    [InlineData("{\"username\": \"\", \"password\": \"\"}")]
    [InlineData("{\"username\": null, \"password\": null}")]
    public async Task Post_Login_NotValidData_ReturnsValidationResult(string content)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_LOGIN_URL);
        request.Headers.Add("Accept-Language", "ru");

        // Тело запроса
        var json = new StringContent(content, Encoding.UTF8, Application.Json);
        request.Content = json;

        // Act
        using var result = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Произошла одна или несколько ошибок проверки.", jsonDocument.RootElement.GetProperty("title").GetString());
    }


    [Theory]
    [InlineData("{\"firstname\": \"\", \"username\": \"\", \"languageCode\": \"\"}")]
    [InlineData("{\"firstname\": null, \"username\": null, \"languageCode\": null}")]
    public async Task Put_User_NotValidData_WhenClaimUserIdEmpty_ReturnsValidationResult(string content)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, TestConstants.USER_URL);
        request.Headers.Add("Accept-Language", "ru");

        // Авторизация с пустым GUID
        TestConstants.AddBearerToken(request, _tokenManager, userId: Guid.Empty.ToString());
        TestConstants.AddIdempotencyKey(request);

        // Тело запроса
        var json = new StringContent(content, Encoding.UTF8, Application.Json);
        request.Content = json;

        // Act
        using var result = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Произошла одна или несколько ошибок проверки.", jsonDocument.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Put_User_ValidData_WhenClaimUserIdEmpty_ReturnsEmptyGuid()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, TestConstants.USER_URL);
        request.Headers.Add("Accept-Language", "ru");

        // Авторизация с пустым GUID
        TestConstants.AddBearerToken(request, _tokenManager, userId: Guid.Empty.ToString());
        TestConstants.AddIdempotencyKey(request);

        // Тело запроса
        var json = new StringContent("{\"firstname\": \"имя\", \"username\": \"some\", \"languageCode\": \"ru\"}", Encoding.UTF8, Application.Json);
        request.Content = json;

        // Act
        using var result = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Пустой уникальный идентификатор (GUID).", jsonDocument.RootElement.GetProperty("title").GetString());
    }

    [Theory]
    [InlineData("{\"firstname\": \"\", \"username\": \"\", \"languageCode\": \"\"}")]
    [InlineData("{\"firstname\": null, \"username\": null, \"languageCode\": null}")]
    public async Task Put_User_NotValidData_WhenClaimUserIdNotEmpty_ReturnsValidationResult(string content)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, TestConstants.USER_URL);
        request.Headers.Add("Accept-Language", "ru");

        // Авторизация
        TestConstants.AddBearerToken(request, _tokenManager);
        TestConstants.AddIdempotencyKey(request);

        // Тело запроса
        var json = new StringContent(content, Encoding.UTF8, Application.Json);
        request.Content = json;

        // Act
        using var result = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Произошла одна или несколько ошибок проверки.", jsonDocument.RootElement.GetProperty("title").GetString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("something")]
    [InlineData(" ")]
    public async Task Put_User_NotValidData_WhenClaimUserIdIsAnyString_ReturnsValidationResult(string id)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, TestConstants.USER_URL);
        request.Headers.Add("Accept-Language", "ru");

        // Авторизация
        TestConstants.AddBearerToken(request, _tokenManager, userId: id);
        TestConstants.AddIdempotencyKey(request);

        // Тело запроса
        var json = new StringContent("{\"firstname\": \"\", \"username\": \"\", \"languageCode\": \"\"}", Encoding.UTF8, Application.Json);
        request.Content = json;

        // Act
        using var result = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Произошла одна или несколько ошибок проверки.", jsonDocument.RootElement.GetProperty("title").GetString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("something")]
    [InlineData(" ")]
    public async Task Put_User_ValidData_WhenClaimUserIdIsAnyString_ReturnsUnauthorized(string id)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, TestConstants.USER_URL);
        request.Headers.Add("Accept-Language", "ru");

        // Авторизация
        TestConstants.AddBearerToken(request, _tokenManager, userId: id);
        TestConstants.AddIdempotencyKey(request);

        // Тело запроса
        var json = new StringContent("{\"firstname\": \"имя\", \"username\": \"some\", \"languageCode\": \"ru\"}", Encoding.UTF8, Application.Json);
        request.Content = json;

        // Act
        using var result = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, result.StatusCode);
    }
}