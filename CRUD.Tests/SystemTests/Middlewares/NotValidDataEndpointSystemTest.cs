using Microsoft.AspNetCore.Hosting;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CRUD.Tests.SystemTests.Middlewares;

public class NotValidDataEndpointSystemTest : IClassFixture<TestWebApplicationFactory>
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
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

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
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

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
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

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
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

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
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

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
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task Put_User_ThrowsInvalidOperationException_NotValidBeforeUpdate()
    {
        // Arrange
        var db = TestWebApplicationFactory.RecreateDatabase();
        var client = _factory.HttpClient;

        // Данные
        string firstname = "новоеИмя";
        string username = "newusername";
        string languageCode = "nn";
        var data = new UpdateUserDto()
        {
            Firstname = firstname,
            Username = username,
            LanguageCode = languageCode
        };

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(db, role: "НЕВАЛИДНАЯ РОЛЬ");

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Put, TestConstants.USER_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());
        TestConstants.AddIdempotencyKey(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);
    }
}