using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;

namespace CRUD.Tests.SystemTests;

public class ConfirmationsSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;

    public ConfirmationsSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
    }


    [Fact]
    public async Task Get_Email_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var confirmEmailRequest = await DI.CreateConfirmEmailRequestAsync(_db, userIdGuid);

        // Запрос
        var url = string.Format(TestConstants.CONFIRMATIONS_EMAIL_TOKEN_URL, confirmEmailRequest.Token);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Почта и вправду подтвердилась
        var userFromDbAfter = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.NotNull(userFromDbAfter);
        Assert.True(userFromDbAfter.IsEmailConfirm);
    }

    [Fact]
    public async Task Get_Email_ReturnsInvalidToken()
    {
        // Arrange
        var client = _factory.HttpClient;

        var token = "something";

        // Запрос
        var url = string.Format(TestConstants.CONFIRMATIONS_EMAIL_TOKEN_URL, token);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.INVALID_TOKEN, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_Email_WhenUserDeleted_ReturnsInvalidToken()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var confirmEmailRequest = await DI.CreateConfirmEmailRequestAsync(_db, userIdGuid);
        var token = confirmEmailRequest.Token;

        // Удаляем пользователя
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        // Запрос
        var url = string.Format(TestConstants.CONFIRMATIONS_EMAIL_TOKEN_URL, token);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.INVALID_TOKEN, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_Email_ReturnsUserAlreadyConfirmedEmail()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isEmailConfirm: true);
        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var confirmEmailRequest = await DI.CreateConfirmEmailRequestAsync(_db, userIdGuid);
        var token = confirmEmailRequest.Token;

        // Запрос
        var url = string.Format(TestConstants.CONFIRMATIONS_EMAIL_TOKEN_URL, token);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.USER_ALREADY_CONFIRMED_EMAIL, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    [Fact]
    public async Task Get_Phone_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var verificationPhoneNumberRequest = await DI.CreateVerificationPhoneNumberRequestAsync(_db, userIdGuid);

        // Запрос
        var url = string.Format(TestConstants.CONFIRMATIONS_PHONE_CODE_URL, verificationPhoneNumberRequest.Code);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Телефонный номер и вправду подтвердился
        var userFromDbAfter = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.NotNull(userFromDbAfter);
        Assert.True(userFromDbAfter.IsPhoneNumberConfirm);
    }

    [Fact]
    public async Task Get_Phone_ReturnsInvalidCode()
    {
        // Arrange
        var client = _factory.HttpClient;

        var userIdGuid = Guid.NewGuid();
        string code = "123456";

        // Запрос
        var url = string.Format(TestConstants.CONFIRMATIONS_PHONE_CODE_URL, code);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request, _tokenManager, userId: userIdGuid.ToString());
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.INVALID_CODE, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_Phone_WhenRequestSendAnotherUser_ReturnsInvalidCode()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователей в базу
        var user = await DI.CreateUserAsync(_db);
        var user2 = await DI.CreateUserAsync(_db, username: "test", email: "test@test.test", phoneNumber: "123456789");

        // Добавляем токен в базу. Владелец этого токена первый пользователь
        var verificationPhoneNumberRequest = await DI.CreateVerificationPhoneNumberRequestAsync(_db, user.Id);

        // Запрос
        // Запрос делает второй пользователь (не владелец) с таким же кодом
        var url = string.Format(TestConstants.CONFIRMATIONS_PHONE_CODE_URL, verificationPhoneNumberRequest.Code);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user2.Id.ToString());
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.INVALID_CODE, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_Phone_WhenUserDeleted_ReturnsInvalidCode()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var verificationPhoneNumberRequest = await DI.CreateVerificationPhoneNumberRequestAsync(_db, userIdGuid);
        var code = verificationPhoneNumberRequest.Code;

        // Удаляем пользователя
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        // Запрос тоже должен удалиться вместе с пользователем
        var verificationPhoneNumberRequestFromDb = await _db.VerificationPhoneNumberRequests.FirstOrDefaultAsync(x => x.Id == verificationPhoneNumberRequest.Id);

        // Запрос
        var url = string.Format(TestConstants.CONFIRMATIONS_PHONE_CODE_URL, code);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request, _tokenManager);
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.INVALID_CODE, jsonDocument.RootElement.GetProperty("code").GetString());

        // Запроса тоже нет, как и пользователя
        Assert.Null(verificationPhoneNumberRequestFromDb);
    }

    [Fact]
    public async Task Get_Phone_ReturnsUserAlreadyConfirmedPhone()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPhoneNumberConfirm: true);
        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var verificationPhoneNumberRequest = await DI.CreateVerificationPhoneNumberRequestAsync(_db, userIdGuid);
        var code = verificationPhoneNumberRequest.Code;

        // Запрос
        var url = string.Format(TestConstants.CONFIRMATIONS_PHONE_CODE_URL, code);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request, _tokenManager, userId: userIdGuid.ToString());
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.USER_ALREADY_CONFIRMED_PHONE_NUMBER, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    [Fact]
    public async Task Get_Password_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var changePasswordRequest = await DI.CreateChangePasswordRequestAsync(_db, userIdGuid);

        // Запрос
        var url = string.Format(TestConstants.CONFIRMATIONS_PASSWORD_TOKEN_URL, changePasswordRequest.Token);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Пароль и вправду обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.NotNull(userFromDbAfterUpdate);
        Assert.Equal(changePasswordRequest.HashedNewPassword, userFromDbAfterUpdate.HashedPassword);
    }

    [Fact]
    public async Task Get_Password_ReturnsInvalidToken()
    {
        // Arrange
        var client = _factory.HttpClient;

        var token = "something";

        // Запрос
        var url = string.Format(TestConstants.CONFIRMATIONS_PASSWORD_TOKEN_URL, token);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.INVALID_TOKEN, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_Password_InvalidCreatedAt_ReturnsInvalidToken()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var changePasswordRequest = await DI.CreateChangePasswordRequestAsync(_db, userIdGuid, expires: DateTime.UtcNow.AddDays(-1));
        var token = changePasswordRequest.Token;

        // Запрос
        var url = string.Format(TestConstants.CONFIRMATIONS_PASSWORD_TOKEN_URL, token);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.INVALID_TOKEN, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_Password_WhenUserDeleted_ReturnsInvalidToken()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var changePasswordRequest = await DI.CreateChangePasswordRequestAsync(_db, userIdGuid);
        var token = changePasswordRequest.Token;

        // Удаляем пользователя
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        // Запрос
        var url = string.Format(TestConstants.CONFIRMATIONS_PASSWORD_TOKEN_URL, token);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.INVALID_TOKEN, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    // Конфликты параллельности


    [Fact]
    public async Task Get_Email_ConcurrencyConflict_ReturnsNoContentOrConflictOrInvalidTokenOrUserAlreadyConfirmedEmail()
    {
        // Arrange
        var client = _factory.HttpClient;
        var client2 = _factory.CreateClient();

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем токен в базу
        var confirmEmailRequest = await DI.CreateConfirmEmailRequestAsync(_db, user.Id);

        // Данные для запросов
        var url = string.Format(TestConstants.CONFIRMATIONS_EMAIL_TOKEN_URL, confirmEmailRequest.Token);

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddIdempotencyKeyQuery(request);

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request2, _tokenManager, user.Id.ToString());
        TestConstants.AddIdempotencyKeyQuery(request2);

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
                // Либо Письмо уже отправлено, Conflict
                var errorCode = jsonDocument.RootElement.GetProperty("code").GetString();
                string[] allowedErrors =
                [
                    ErrorCodes.INVALID_TOKEN,
                    ErrorCodes.USER_ALREADY_CONFIRMED_EMAIL,
                    ErrorCodes.CONCURRENCY_CONFLICTS
                ];

                Assert.Contains(errorCode, allowedErrors);
            }
        }
    }


    [Fact]
    public async Task Get_Phone_ConcurrencyConflict_ReturnsNoContentOrConflictOrInvalidCodeOrUserAlreadyConfirmedPhoneNumber()
    {
        // Arrange
        var client = _factory.HttpClient;
        var client2 = _factory.CreateClient();

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем токен в базу
        var verificationPhoneNumberRequest = await DI.CreateVerificationPhoneNumberRequestAsync(_db, user.Id);

        // Данные для запросов
        var url = string.Format(TestConstants.CONFIRMATIONS_PHONE_CODE_URL, verificationPhoneNumberRequest.Code);

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request, _tokenManager, user.Id.ToString());
        TestConstants.AddIdempotencyKeyQuery(request);

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request2, _tokenManager, user.Id.ToString());
        TestConstants.AddIdempotencyKeyQuery(request2);

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
                // Либо письмо уже отправлено, Conflict
                var errorCode = jsonDocument.RootElement.GetProperty("code").GetString();
                string[] allowedErrors =
                [
                    ErrorCodes.INVALID_CODE,
                    ErrorCodes.USER_ALREADY_CONFIRMED_PHONE_NUMBER,
                    ErrorCodes.CONCURRENCY_CONFLICTS
                ];

                Assert.Contains(errorCode, allowedErrors);
            }
        }
    }


    [Fact]
    public async Task Get_Password_ConcurrencyConflict_ReturnsNoContentOrConflictOrInvalidToken()
    {
        // Arrange
        var client = _factory.HttpClient;
        var client2 = _factory.CreateClient();

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем токен в базу
        var changePasswordRequest = await DI.CreateChangePasswordRequestAsync(_db, user.Id);

        // Данные для запросов
        var url = string.Format(TestConstants.CONFIRMATIONS_PASSWORD_TOKEN_URL, changePasswordRequest.Token);

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddIdempotencyKeyQuery(request);

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddIdempotencyKeyQuery(request2);

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
                // Либо письмо уже отправлено, Conflict
                var errorCode = jsonDocument.RootElement.GetProperty("code").GetString();
                string[] allowedErrors =
                [
                    ErrorCodes.INVALID_TOKEN,
                    ErrorCodes.CONCURRENCY_CONFLICTS
                ];

                Assert.Contains(errorCode, allowedErrors);
            }
        }
    }
}