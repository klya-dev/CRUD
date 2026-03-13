using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CRUD.Tests.SystemTests.User;

public class UserSystemTest : IClassFixture<TestWebApplicationFactory>
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
        var user = await DI.CreateUserAsync(_db);
        var expectedDto = new UserDto()
        {
            Username = user.Username,
            Firstname = user.Firstname,
            LanguageCode = user.LanguageCode
        };

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_URL);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

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
    public async Task Get_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_URL);
        TestConstants.AddBearerToken(request, _tokenManager);

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


    [Theory] // Корректные данные
    [InlineData("новоеИмя", "newusername", "nn")]
    [InlineData("Кля", "username", "en")] // Меняем всё кроме username'а
    public async Task Put_ReturnsNoContent(string newFirstname, string newUsername, string newLanguageCode)
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Данные
        var data = new UpdateUserDto()
        {
            Firstname = newFirstname,
            Username = newUsername,
            LanguageCode = newLanguageCode
        };

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
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Пользователь и вправду обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == user.Id);
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
        var request = new HttpRequestMessage(HttpMethod.Put, TestConstants.USER_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager);
        TestConstants.AddIdempotencyKey(request);

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
        var user = await DI.CreateUserAsync(_db, firstname: firstname, username: username, languageCode: languageCode);

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
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

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
        var user = await DI.CreateUserAsync(_db, username: "username");

        // Добавляем пользователя в базу
        var user2 = await DI.CreateUserAsync(_db, username: username, email: "test", phoneNumber: "1234567");

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
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.USERNAME_ALREADY_TAKEN, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Данные
        string password = "123";
        var data = new DeleteUserDto()
        {
            Password = password
        };

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Delete, TestConstants.USER_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());
        TestConstants.AddIdempotencyKey(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        var userFromDbAfterDelete = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);
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
        var request = new HttpRequestMessage(HttpMethod.Delete, TestConstants.USER_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager);
        TestConstants.AddIdempotencyKey(request);

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
    public async Task Delete_ReturnsInvalidPassword()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Данные
        string password = "123";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, hashedPassword: password);

        var data = new DeleteUserDto()
        {
            Password = "12345"
        };

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Delete, TestConstants.USER_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());
        TestConstants.AddIdempotencyKey(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.INVALID_PASSWORD, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    // Конфликты параллельности


    [Fact]
    public async Task Get_ConcurrencyConflict_ReturnsUserDto()
    {
        // Arrange
        var client = _factory.HttpClient;
        var client2 = _factory.CreateClient();

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var expectedDto = new UserDto()
        {
            Username = user.Username,
            Firstname = user.Firstname,
            LanguageCode = user.LanguageCode
        };

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_URL);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_URL);
        TestConstants.AddBearerToken(request2, _tokenManager, userId: user.Id.ToString());

        // Act
        using var task = client.SendAsync(request);
        using var task2 = client2.SendAsync(request2);

        var results = await Task.WhenAll(task, task2);

        // Assert
        foreach (var result in results)
        {
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
    }


    [Theory] // Корректные данные
    [InlineData("новоеИмя", "newusername", "nn")]
    [InlineData("Кля", "username", "en")] // Меняем всё кроме username'а
    public async Task Put_ConcurrencyConflict_ReturnsNoContentOrConflictOrNoChangesDetectedOrUsernameAlreadyTaken(string newFirstname, string newUsername, string newLanguageCode)
    {
        // Arrange
        var client = _factory.HttpClient;
        var client2 = _factory.CreateClient();

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Данные для запросов
        var data = new UpdateUserDto()
        {
            Firstname = newFirstname,
            Username = newUsername,
            LanguageCode = newLanguageCode
        };

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Put, TestConstants.USER_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());
        TestConstants.AddIdempotencyKey(request);

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Put, TestConstants.USER_URL);
        var json2 = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request2.Content = json2;
        TestConstants.AddBearerToken(request2, _tokenManager, userId: user.Id.ToString());
        TestConstants.AddIdempotencyKey(request2);

        // Act
        using var task = client.SendAsync(request);
        using var task2 = client2.SendAsync(request2);

        var results = await Task.WhenAll(task, task2);

        // Assert
        foreach (var result in results)
        {
            Assert.NotNull(result);

            // Ошибка сервера
            if (System.Net.HttpStatusCode.InternalServerError == result.StatusCode)
                Assert.Fail("InternalServerError");

            // Может быть успешный ответ
            if (System.Net.HttpStatusCode.NoContent == result.StatusCode)
            {
                Assert.Null(result.Content.Headers.ContentType);

                // Пользователь и вправду обновился
                var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);
                Assert.Equal(newFirstname, userFromDbAfterUpdate.Firstname);
                Assert.Equal(newUsername, userFromDbAfterUpdate.Username);
                Assert.Equal(newLanguageCode, userFromDbAfterUpdate.LanguageCode);

                continue;
            }

            // Читаем содержимое ответа
            await using var contentStream = await result.Content.ReadAsStreamAsync();
            using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

            // Может быть неуспешный ответ
            if (!result.IsSuccessStatusCode)
            {
                // Либо Username, Email, PhoneNumber уже занят, Conflict
                var errorCode = jsonDocument.RootElement.GetProperty("code").GetString();
                string[] allowedErrors =
                [
                    ErrorCodes.NO_CHANGES_DETECTED,
                    ErrorCodes.USERNAME_ALREADY_TAKEN,
                    ErrorCodes.EMAIL_ALREADY_TAKEN,
                    ErrorCodes.PHONE_NUMBER_ALREADY_TAKEN,
                    ErrorCodes.CONCURRENCY_CONFLICTS
                ];

                Assert.Contains(errorCode, allowedErrors);
            }
        }
    }

    [Fact]
    public async Task Delete_ConcurrencyConflict_ReturnsNoContentOrConflictOrUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;
        var client2 = _factory.CreateClient();

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Данные для запросов
        string password = "123";
        var data = new DeleteUserDto()
        {
            Password = password
        };

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Delete, TestConstants.USER_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());
        TestConstants.AddIdempotencyKey(request);

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Delete, TestConstants.USER_URL);
        var json2 = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request2.Content = json2;
        TestConstants.AddBearerToken(request2, _tokenManager, userId: user.Id.ToString());
        TestConstants.AddIdempotencyKey(request2);

        // Act
        using var task = client.SendAsync(request);
        using var task2 = client2.SendAsync(request2);

        var results = await Task.WhenAll(task, task2);

        // Assert
        foreach (var result in results)
        {
            Assert.NotNull(result);

            // Ошибка сервера
            if (System.Net.HttpStatusCode.InternalServerError == result.StatusCode)
                Assert.Fail("InternalServerError");

            // Может быть успешный ответ
            if (System.Net.HttpStatusCode.NoContent == result.StatusCode)
            {
                Assert.Null(result.Content.Headers.ContentType);

                var userFromDbAfterDelete = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);
                Assert.Null(userFromDbAfterDelete);

                continue;
            }

            // Читаем содержимое ответа
            await using var contentStream = await result.Content.ReadAsStreamAsync();
            using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

            // Может быть неуспешный ответ
            if (!result.IsSuccessStatusCode)
            {
                // Либо Пользователь не найден, либо Conflict
                var errorCode = jsonDocument.RootElement.GetProperty("code").GetString();
                string[] allowedErrors =
                [
                    ErrorCodes.USER_NOT_FOUND,
                    ErrorCodes.CONCURRENCY_CONFLICTS
                ];

                Assert.Contains(errorCode, allowedErrors);
            }
        }
    }
}