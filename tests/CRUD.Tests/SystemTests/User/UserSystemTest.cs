using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CRUD.Tests.SystemTests.User;

[Collection(nameof(IntegrationsTestCollection))]
public sealed class UserSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;

    public UserSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
    }

    [Fact]
    public async Task Get_ReturnsUserDto()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var expectedDto = new UserDto()
        {
            Username = user.Username,
            Firstname = user.Firstname,
            LanguageCode = user.LanguageCode,
            AvatarPresignedUrl = "something"
        };

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_URL);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

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
    public async Task Get_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_URL);
        TestConstants.AddBearerToken(request, _tokenManager);

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


    [Theory] // Корректные данные
    [InlineData("новоеИмя", "newusername", "nn")]
    [InlineData("Кля", "username", "en")] // Меняем всё кроме username'а
    public async Task Put_ReturnsNoContent(string newFirstname, string newUsername, string newLanguageCode)
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Данные
        var data = new UpdateUserDto()
        {
            Firstname = newFirstname,
            Username = newUsername,
            LanguageCode = newLanguageCode
        };

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Put, TestConstants.USER_URL)
        {
            Content = JsonContent.Create(data)
        };
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());
        TestConstants.AddIdempotencyKey(request);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Пользователь и вправду обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
        Assert.Equal(newFirstname, userFromDbAfterUpdate.Firstname);
        Assert.Equal(newUsername, userFromDbAfterUpdate.Username);
        Assert.Equal(newLanguageCode, userFromDbAfterUpdate.LanguageCode);
    }

    // NotValidBeforeUpdate в NotValidDataEndpointSystemTest

    [Fact]
    public async Task Put_ReturnsUserNotFound()
    {
        // Arrange
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

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Put, TestConstants.USER_URL)
        {
            Content = JsonContent.Create(data)
        };
        TestConstants.AddBearerToken(request, _tokenManager);
        TestConstants.AddIdempotencyKey(request);

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
    public async Task Put_ReturnsNoChangesDetected()
    {
        // Arrange
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
        var user = await DI.CreateUserAsync(_db, firstname: firstname, username: username, languageCode: languageCode, ct: TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Put, TestConstants.USER_URL)
        {
            Content = JsonContent.Create(data)
        };
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());
        TestConstants.AddIdempotencyKey(request);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.NO_CHANGES_DETECTED, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Put_ReturnsUsernameAlreadyTaken()
    {
        // Arrange
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
        var user = await DI.CreateUserAsync(_db, username: "username", ct: TestContext.Current.CancellationToken);

        // Добавляем пользователя в базу
        var user2 = await DI.CreateUserAsync(_db, username: username, email: "test", phoneNumber: "1234567", ct: TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Put, TestConstants.USER_URL)
        {
            Content = JsonContent.Create(data)
        };
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());
        TestConstants.AddIdempotencyKey(request);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.USERNAME_ALREADY_TAKEN, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Данные
        string password = "123";
        var data = new DeleteUserDto()
        {
            Password = password
        };

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Delete, TestConstants.USER_URL)
        {
            Content = JsonContent.Create(data)
        };
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());
        TestConstants.AddIdempotencyKey(request);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        var userFromDbAfterDelete = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
        Assert.Null(userFromDbAfterDelete);
    }

    [Fact]
    public async Task Delete_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Данные
        string password = "123";
        var data = new DeleteUserDto()
        {
            Password = password
        };

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Delete, TestConstants.USER_URL)
        {
            Content = JsonContent.Create(data)
        };
        TestConstants.AddBearerToken(request, _tokenManager);
        TestConstants.AddIdempotencyKey(request);

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
    public async Task Delete_ReturnsInvalidPassword()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Данные
        string password = "123";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, hashedPassword: password, ct: TestContext.Current.CancellationToken);

        var data = new DeleteUserDto()
        {
            Password = "12345"
        };

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Delete, TestConstants.USER_URL)
        {
            Content = JsonContent.Create(data)
        };
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());
        TestConstants.AddIdempotencyKey(request);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.INVALID_PASSWORD, jsonDocument.RootElement.GetProperty("code").GetString());
    }
}