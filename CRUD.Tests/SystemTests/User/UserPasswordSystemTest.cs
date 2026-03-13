using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CRUD.Tests.SystemTests.User;

public class UserPasswordSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;
    private readonly IPasswordHasher _passwordHasher;

    public UserPasswordSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
        _passwordHasher = scopedServices.GetRequiredService<IPasswordHasher>();
    }

    [Theory]
    [InlineData("123", "!123@L")]
    [InlineData("kekpass", "newsuperpassword")]
    public async Task Post_ReturnsNoContent(string password, string newPassword)
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, hashedPassword: password);

        // Данные
        var data = new ChangePasswordDto()
        {
            Password = password,
            NewPassword = newPassword
        };

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_PASSWORD_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Пароль и вправду не обновился (т.к нужно подтверждение)
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);
        Assert.False(_passwordHasher.Verify(data.NewPassword, userFromDbAfterUpdate.HashedPassword));
    }

    [Fact]
    public async Task Post_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Данные
        string password = "123";
        string newPassword = "!123@L";
        var data = new ChangePasswordDto()
        {
            Password = password,
            NewPassword = newPassword
        };

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_PASSWORD_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
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

    [Fact]
    public async Task Post_ReturnsInvalidPassword()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Данные
        string password = "123";
        string newPassword = "!123@L";
        var data = new ChangePasswordDto()
        {
            Password = password,
            NewPassword = newPassword
        };

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, hashedPassword: password + "SOMETHING_WRONG");

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_PASSWORD_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);

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

        // Пароль и вправду не обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);
        Assert.Equivalent(userFromDbBeforeUpdate, userFromDbAfterUpdate);
    }

    [Fact]
    public async Task Post_ReturnsLetterAlreadySent()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Данные
        string password = "123";
        string newPassword = "!123@L";
        var data = new ChangePasswordDto()
        {
            Password = password,
            NewPassword = newPassword
        };

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, hashedPassword: password);

        // Добавляем токен в базу, чтобы возникла ошибка, что письмо уже отправлено
        var changePasswordRequest = await DI.CreateChangePasswordRequestAsync(_db, user.Id);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_PASSWORD_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.LETTER_ALREADY_SENT, jsonDocument.RootElement.GetProperty("code").GetString());

        // Пароль и вправду не обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);
        Assert.Equivalent(userFromDbBeforeUpdate, userFromDbAfterUpdate);
    }


    // Конфликты параллельности


    [Theory]
    [InlineData("123", "!123@L")]
    [InlineData("kekpass", "newsuperpassword")]
    public async Task Post_ConcurrencyConfict_ReturnsNothingOrConflictOrInvalidPasswordOrLetterAlreadySent(string password, string newPassword)
    {
        // Arrange
        var client = _factory.HttpClient;
        var client2 = _factory.CreateClient();

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, hashedPassword: password);

        // Данные для запросов
        var data = new ChangePasswordDto()
        {
            Password = password,
            NewPassword = newPassword
        };

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_PASSWORD_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_PASSWORD_URL);
        var json2 = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request2.Content = json2;
        TestConstants.AddBearerToken(request2, _tokenManager, userId: user.Id.ToString());

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

                continue;
            }

            // Читаем содержимое ответа
            await using var contentStream = await result.Content.ReadAsStreamAsync();
            using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

            // Может быть неуспешный ответ
            if (!result.IsSuccessStatusCode)
            {
                // Либо Неверный пароль, либо Письмо уже отправлено, либо Conflict
                var errorCode = jsonDocument.RootElement.GetProperty("code").GetString();
                string[] allowedErrors =
                [
                    ErrorCodes.INVALID_PASSWORD,
                    ErrorCodes.LETTER_ALREADY_SENT,
                    ErrorCodes.CONCURRENCY_CONFLICTS
                ];

                Assert.Contains(errorCode, allowedErrors);
            }
        }

        // Пароль и вправду не обновился (т.к нужно подтверждение)
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);
        Assert.False(_passwordHasher.Verify(data.NewPassword, userFromDbAfterUpdate.HashedPassword));
    }
}