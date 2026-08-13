using CRUD.Models.Domains;
using System.Text.Json;

namespace CRUD.Tests.SystemTests;

[Collection(nameof(IntegrationsTestCollection))]
public sealed class UsersSystemTest : IClassFixture<TestWebApplicationFactory>
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
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var expectedDto = new UserDto()
        {
            Firstname = user.Firstname,
            Username = user.Username,
            LanguageCode = user.LanguageCode,
            AvatarPresignedUrl = "something"
        };

        // Запрос
        var url = string.Format(TestConstants.USERS_USER_ID_URL, user.Id);
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);
        var response = jsonDocument.RootElement.Deserialize<UserDto>();

        Assert.NotNull(response);
        Assert.NotNull(response.Firstname);
        Assert.NotNull(response.Username);
        Assert.NotNull(response.LanguageCode);

        AssertExtensions.EqualIgnoring(expectedDto, response, ignoreProperties: nameof(UserDto.AvatarPresignedUrl)); // AvatarPresignedUrl не сравниваем
        Assert.StartsWith("https://", response.AvatarPresignedUrl); // Нормальная ссылка
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
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.USER_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    [Fact]
    public async Task Get_UserId_AvatarFile_ReturnsFileStream()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Запрос
        var url = string.Format(TestConstants.USERS_USER_ID_AVATAR_FILE_URL, user.Id);
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/octet-stream", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);

        Assert.True(contentStream.Length > 0);
    }

    [Fact]
    public async Task Get_UserId_AvatarFile_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Запрос
        var url = string.Format(TestConstants.USERS_USER_ID_AVATAR_FILE_URL, Guid.NewGuid());
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.USER_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_UserId_AvatarFile_ReturnsFileNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: "something", ct: TestContext.Current.CancellationToken);

        // Запрос
        var url = string.Format(TestConstants.USERS_USER_ID_AVATAR_FILE_URL, user.Id);
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.FILE_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    [Fact]
    public async Task Get_UserId_AvatarUrl_ReturnsUrl()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Запрос
        var url = string.Format(TestConstants.USERS_USER_ID_AVATAR_URL_URL, user.Id);
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        var contentString = await result.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        AssertExtensions.IsNotNullOrNotWhiteSpace(contentString);
    }

    [Fact]
    public async Task Get_UserId_AvatarUrl_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Запрос
        var url = string.Format(TestConstants.USERS_USER_ID_AVATAR_URL_URL, Guid.NewGuid());
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.USER_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_UserId_AvatarUrl_WhenFileNotExists_ReturnsUrl()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: "something", ct: TestContext.Current.CancellationToken);

        // Запрос
        var url = string.Format(TestConstants.USERS_USER_ID_AVATAR_URL_URL, user.Id);
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        var contentString = await result.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        AssertExtensions.IsNotNullOrNotWhiteSpace(contentString);
    }
}