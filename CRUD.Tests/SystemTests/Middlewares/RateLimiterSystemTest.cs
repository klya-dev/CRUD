using CRUD.Utility.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CRUD.Tests.SystemTests.Middlewares;

public class RateLimiterSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;

    public RateLimiterSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;

        // https://stackoverflow.com/questions/72679169/override-host-configuration-in-integration-testing-using-asp-net-core-6-minimal
        var dict = new Dictionary<string, string>
        {
            // Глобальный: 1 запрос в 10 секунд
            [$"{RateLimiterOptions.SectionName}:{nameof(RateLimiterOptions.Global)}:{nameof(RateLimiterOptions.Global.PermitLimit)}"] = "1",
            [$"{RateLimiterOptions.SectionName}:{nameof(RateLimiterOptions.Global)}:{nameof(RateLimiterOptions.Global.Window)}"] = "10",
            [$"{RateLimiterOptions.SectionName}:{nameof(RateLimiterOptions.Global)}:{nameof(RateLimiterOptions.Global.QueueLimit)}"] = "0",

            // Для "/publications... GET": 2 запроса в 11 секунд
            [$"{RateLimiterOptions.SectionName}:{nameof(RateLimiterOptions.PublicationsGet)}:{nameof(RateLimiterOptions.PublicationsGet.PermitLimit)}"] = "2",
            [$"{RateLimiterOptions.SectionName}:{nameof(RateLimiterOptions.PublicationsGet)}:{nameof(RateLimiterOptions.PublicationsGet.Window)}"] = "11",
            [$"{RateLimiterOptions.SectionName}:{nameof(RateLimiterOptions.PublicationsGet)}:{nameof(RateLimiterOptions.PublicationsGet.QueueLimit)}"] = "0"
        }; 
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();

        _client = factory.WithWebHostBuilder(builder =>
        {
            // Конфигурация до того, как будет вызван WebApplication.CreateBuilder(args);
            builder.UseConfiguration(configuration);
            builder.ConfigureAppConfiguration((ctx, config) =>
            {
                // Переопределяет значения после WebApplication.CreateBuilder(args);
                config.AddInMemoryCollection(dict);
            });
        }).CreateClient();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
    }

    [Fact]
    public async Task Post_Login_Global_ReturnsTooManyRequests()
    {
        // Arrange

        // Тело запроса
        LoginDataDto loginData = new() { Username = "noob", Password = "123"};
        var json = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, Application.Json);

        // Act
        // 1 запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_LOGIN_URL);
        request.Headers.Add("Accept-Language", "ru");
        request.Content = json;
        using var result1 = await _client.SendAsync(request);

        // 2 запрос
        var request2 = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_LOGIN_URL);
        request2.Headers.Add("Accept-Language", "ru");
        request2.Content = json;
        using var result2 = await _client.SendAsync(request2);

        // Assert
        Assert.NotNull(result1);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, result1.StatusCode);
        Assert.Null(result1.Headers.RetryAfter);


        Assert.NotNull(result2);
        Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, result2.StatusCode);
        Assert.Equal("application/problem+json", result2.Content.Headers.ContentType?.MediaType);
        Assert.Equal("10", result2.Headers.RetryAfter.ToString());

        // Читаем содержимое ответа
        await using var contentStream = await result2.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Превышен лимит скорости, слишком много запросов. Попробуйте позже.", jsonDocument.RootElement.GetProperty("detail").GetString());
        Assert.Equal(ErrorCodes.RATE_LIMIT_EXCEEDED, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_User_GlobalWhenUserIsAuth_ReturnsTooManyRequests()
    {
        TestWebApplicationFactory.RecreateDatabase();

        // Arrange

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Act
        // 1 запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_URL);
        request.Headers.Add("Accept-Language", "ru");
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());
        using var result1 = await _client.SendAsync(request);

        // 2 запрос
        var request2 = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_URL);
        request2.Headers.Add("Accept-Language", "ru");
        TestConstants.AddBearerToken(request2, _tokenManager, userId: user.Id.ToString());
        using var result2 = await _client.SendAsync(request2);

        // Assert
        Assert.NotNull(result1);
        Assert.Equal(System.Net.HttpStatusCode.OK, result1.StatusCode);
        Assert.Null(result1.Headers.RetryAfter);


        Assert.NotNull(result2);
        Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, result2.StatusCode);
        Assert.Equal("application/problem+json", result2.Content.Headers.ContentType?.MediaType);
        Assert.Equal("10", result2.Headers.RetryAfter.ToString());

        // Читаем содержимое ответа
        await using var contentStream = await result2.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Превышен лимит скорости, слишком много запросов. Попробуйте позже.", jsonDocument.RootElement.GetProperty("detail").GetString());
        Assert.Equal(ErrorCodes.RATE_LIMIT_EXCEEDED, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_User_GlobalWhenUserIsAuthAdmin_ReturnsOk()
    {
        TestWebApplicationFactory.RecreateDatabase();

        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Act
        // 1 запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_URL);
        request.Headers.Add("Accept-Language", "ru");
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString(), role: UserRoles.Admin);
        using var result1 = await _client.SendAsync(request);

        // 2 запрос
        var request2 = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_URL);
        request2.Headers.Add("Accept-Language", "ru");
        TestConstants.AddBearerToken(request2, _tokenManager, userId: user.Id.ToString(), role: UserRoles.Admin);
        using var result2 = await _client.SendAsync(request2);

        // Assert
        Assert.NotNull(result1);
        Assert.Equal(System.Net.HttpStatusCode.OK, result1.StatusCode);
        Assert.Null(result1.Headers.RetryAfter);


        Assert.NotNull(result2);
        Assert.Equal(System.Net.HttpStatusCode.OK, result2.StatusCode);
        Assert.Equal("application/json", result2.Content.Headers.ContentType?.MediaType);
        Assert.Null(result2.Headers.RetryAfter);
    }

    [Fact]
    public async Task Get_User_GlobalWhenUserIsNotAuth_ReturnsTooManyRequests()
    {
        // Arrange

        // Act
        // 1 запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_URL);
        request.Headers.Add("Accept-Language", "ru");
        using var result1 = await _client.SendAsync(request);

        // 2 запрос
        var request2 = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_URL);
        request2.Headers.Add("Accept-Language", "ru");
        using var result2 = await _client.SendAsync(request2);

        // Assert
        Assert.NotNull(result1);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, result1.StatusCode);
        Assert.Null(result1.Headers.RetryAfter);


        Assert.NotNull(result2);
        Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, result2.StatusCode);
        Assert.Equal("application/problem+json", result2.Content.Headers.ContentType?.MediaType);
        Assert.Equal("10", result2.Headers.RetryAfter.ToString());

        // Читаем содержимое ответа
        await using var contentStream = await result2.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Превышен лимит скорости, слишком много запросов. Попробуйте позже.", jsonDocument.RootElement.GetProperty("detail").GetString());
        Assert.Equal(ErrorCodes.RATE_LIMIT_EXCEEDED, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_Publications_Authors_AuthorId_GlobalPublicationsGet_ReturnsTooManyRequests()
    {
        // Arrange
        var url = string.Format(TestConstants.PUBLICATIONS_AUTHORS_AUTHOR_ID_URL, Guid.NewGuid()) + "?count=1";

        // Act
        // 1 запрос
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept-Language", "ru");
        using var result1 = await _client.SendAsync(request);

        // 2 запрос
        var request2 = new HttpRequestMessage(HttpMethod.Get, url);
        request2.Headers.Add("Accept-Language", "ru");
        using var result2 = await _client.SendAsync(request2);

        // 3 запрос
        var request3 = new HttpRequestMessage(HttpMethod.Get, url);
        request3.Headers.Add("Accept-Language", "ru");
        var result3 = await _client.SendAsync(request3);

        // Assert
        Assert.NotNull(result1);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result1.StatusCode);
        Assert.Null(result1.Headers.RetryAfter);

        Assert.NotNull(result2);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result2.StatusCode);
        Assert.Null(result1.Headers.RetryAfter);


        Assert.NotNull(result3);
        Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, result3.StatusCode);
        Assert.Equal("application/problem+json", result3.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(result3.Headers.RetryAfter);
        Assert.Equal("11", result3.Headers.RetryAfter.ToString());

        // Читаем содержимое ответа
        await using var contentStream = await result3.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Превышен лимит скорости, слишком много запросов. Попробуйте позже.", jsonDocument.RootElement.GetProperty("detail").GetString());
        Assert.Equal(ErrorCodes.RATE_LIMIT_EXCEEDED, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_Metrics_ReturnsOk()
    {
        // Arrange

        // Act
        // 1 запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.METRICS_URL);
        request.Headers.Add("Accept-Language", "ru");
        using var result1 = await _client.SendAsync(request);

        // 2 запрос
        var request2 = new HttpRequestMessage(HttpMethod.Get, TestConstants.METRICS_URL);
        request2.Headers.Add("Accept-Language", "ru");
        using var result2 = await _client.SendAsync(request2);

        // Assert
        Assert.NotNull(result1);
        Assert.Equal(System.Net.HttpStatusCode.OK, result1.StatusCode);
        Assert.Null(result1.Headers.RetryAfter);

        Assert.NotNull(result2);
        Assert.Equal(System.Net.HttpStatusCode.OK, result2.StatusCode);
        Assert.Null(result2.Headers.RetryAfter);
    }
}