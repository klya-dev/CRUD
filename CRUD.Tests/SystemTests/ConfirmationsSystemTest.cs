using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;

namespace CRUD.Tests.SystemTests;

[Collection(nameof(IntegrationsTestCollection))]
public sealed class ConfirmationsSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;
    private readonly ITimeLimitedDataProtector _protector;

    public ConfirmationsSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
        _protector = scopedServices.GetRequiredService<IDataProtectionProvider>().CreateProtector(ChangePasswordRequestManager.Purpose).ToTimeLimitedDataProtector();
    }


    [Fact]
    public async Task Get_Email_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var confirmEmailRequest = await DI.CreateConfirmEmailRequestAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);

        // Запрос
        var url = string.Format(TestConstants.CONFIRMATIONS_EMAIL_TOKEN_URL, confirmEmailRequest.Token);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Почта и вправду подтвердилась
        var userFromDbAfter = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
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
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.INVALID_TOKEN, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_Email_WhenUserDeleted_ReturnsInvalidToken()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var confirmEmailRequest = await DI.CreateConfirmEmailRequestAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);
        var token = confirmEmailRequest.Token;

        // Удаляем пользователя
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Запрос
        var url = string.Format(TestConstants.CONFIRMATIONS_EMAIL_TOKEN_URL, token);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.INVALID_TOKEN, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_Email_ReturnsUserAlreadyConfirmedEmail()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isEmailConfirm: true, ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var confirmEmailRequest = await DI.CreateConfirmEmailRequestAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);
        var token = confirmEmailRequest.Token;

        // Запрос
        var url = string.Format(TestConstants.CONFIRMATIONS_EMAIL_TOKEN_URL, token);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.USER_ALREADY_CONFIRMED_EMAIL, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    [Fact]
    public async Task Get_Phone_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var verificationPhoneNumberRequest = await DI.CreateVerificationPhoneNumberRequestAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);

        // Запрос
        var url = string.Format(TestConstants.CONFIRMATIONS_PHONE_CODE_URL, verificationPhoneNumberRequest.Code);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Телефонный номер и вправду подтвердился
        var userFromDbAfter = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
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
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.INVALID_CODE, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_Phone_WhenRequestSendAnotherUser_ReturnsInvalidCode()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователей в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var user2 = await DI.CreateUserAsync(_db, username: "test", email: "test@test.test", phoneNumber: "123456789", ct: TestContext.Current.CancellationToken);

        // Добавляем токен в базу. Владелец этого токена первый пользователь
        var verificationPhoneNumberRequest = await DI.CreateVerificationPhoneNumberRequestAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        // Запрос
        // Запрос делает второй пользователь (не владелец) с таким же кодом
        var url = string.Format(TestConstants.CONFIRMATIONS_PHONE_CODE_URL, verificationPhoneNumberRequest.Code);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user2.Id.ToString());
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.INVALID_CODE, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_Phone_WhenUserDeleted_ReturnsInvalidCode()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var verificationPhoneNumberRequest = await DI.CreateVerificationPhoneNumberRequestAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);
        var code = verificationPhoneNumberRequest.Code;

        // Удаляем пользователя
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Запрос тоже должен удалиться вместе с пользователем
        var verificationPhoneNumberRequestFromDb = await _db.VerificationPhoneNumberRequests.FirstOrDefaultAsync(x => x.Id == verificationPhoneNumberRequest.Id, TestContext.Current.CancellationToken);

        // Запрос
        var url = string.Format(TestConstants.CONFIRMATIONS_PHONE_CODE_URL, code);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request, _tokenManager);
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

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
        var user = await DI.CreateUserAsync(_db, isPhoneNumberConfirm: true, ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var verificationPhoneNumberRequest = await DI.CreateVerificationPhoneNumberRequestAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);
        var code = verificationPhoneNumberRequest.Code;

        // Запрос
        var url = string.Format(TestConstants.CONFIRMATIONS_PHONE_CODE_URL, code);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request, _tokenManager, userId: userIdGuid.ToString());
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.USER_ALREADY_CONFIRMED_PHONE_NUMBER, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    [Fact]
    public async Task Get_Password_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        // Валидный токен
        var payload = DI.CreateChangePasswordPayload(userIdGuid);
        string payloadJson = JsonSerializer.Serialize(payload);
        string token = _protector.Protect(payloadJson, TimeSpan.FromMinutes(1));

        // Запрос
        var url = string.Format(TestConstants.CONFIRMATIONS_PASSWORD_TOKEN_URL, token);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Пароль и вправду обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
        Assert.NotNull(userFromDbAfterUpdate);
        Assert.Equal(payload.HashedNewPassword, userFromDbAfterUpdate.HashedPassword);
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
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.INVALID_TOKEN, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_Password_InvalidCreatedAt_ReturnsInvalidToken()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        // Истёкший токен
        var payload = DI.CreateChangePasswordPayload(userIdGuid);
        string payloadJson = JsonSerializer.Serialize(payload);
        string token = _protector.Protect(payloadJson, TimeSpan.FromMinutes(-60));

        // Запрос
        var url = string.Format(TestConstants.CONFIRMATIONS_PASSWORD_TOKEN_URL, token);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.INVALID_TOKEN, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_Password_WhenUserDeleted_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        // Валидный токен
        var payload = DI.CreateChangePasswordPayload(userIdGuid);
        string payloadJson = JsonSerializer.Serialize(payload);
        string token = _protector.Protect(payloadJson, TimeSpan.FromMinutes(1));

        // Удаляем пользователя
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Запрос
        var url = string.Format(TestConstants.CONFIRMATIONS_PASSWORD_TOKEN_URL, token);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddIdempotencyKeyQuery(request);

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
}