using CRUD.Models.Domains;
using System.Text.Json;

namespace CRUD.Tests.SystemTests;

public class UsersSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;

    public UsersSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
    }

    [Fact]
    public async Task Get_UserId_ReturnsUserDto()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var expectedDto = new UserDto()
        {
            Firstname = user.Firstname,
            Username = user.Username,
            LanguageCode = user.LanguageCode
        };

        // Запрос
        var url = string.Format(TestConstants.USERS_USER_ID_URL, user.Id);
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.RootElement.Deserialize<UserDto>();

        Assert.NotNull(response);
        Assert.NotNull(response.Firstname);
        Assert.NotNull(response.Username);
        Assert.NotNull(response.LanguageCode);

        Assert.Equivalent(expectedDto, response);
    }

    [Fact]
    public async Task Get_UserId_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Запрос
        var url = string.Format(TestConstants.USERS_USER_ID_URL, Guid.NewGuid());
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.USER_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    [Fact]
    public async Task Get_UserId_Avatar_ReturnsFileStream()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Запрос
        var url = string.Format(TestConstants.USERS_USER_ID_AVATAR_URL, user.Id);
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/octet-stream", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();

        Assert.True(contentStream.Length > 0);
    }

    [Fact]
    public async Task Get_UserId_Avatar_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Запрос
        var url = string.Format(TestConstants.USERS_USER_ID_AVATAR_URL, Guid.NewGuid());
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.USER_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_UserId_Avatar_ReturnsFileNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: "something");

        // Запрос
        var url = string.Format(TestConstants.USERS_USER_ID_AVATAR_URL, user.Id);
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.FILE_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }
}