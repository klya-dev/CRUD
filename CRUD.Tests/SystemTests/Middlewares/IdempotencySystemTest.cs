using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CRUD.Tests.SystemTests.Middlewares;

public class IdempotencySystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;

    public IdempotencySystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
    }

    [Fact] // Первый запрос - 204 (пользователь обновился), второй запрос с тем же IdempotencyKey - 204 (пользователь второй раз не обновляется)
    public async Task Put_User_WhenTwoRequestsWithSameIdempotencyKey_ReturnsNoContent()
    {
        // Arrange
        string newFirstname = "новоеИмя";
        string newUsername = "newusername";
        string newLanguageCode = "nn";

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

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Put, TestConstants.USER_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());
        var idmKey = TestConstants.AddIdempotencyKey(request);

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Put, TestConstants.USER_URL);
        var json2 = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request2.Content = json;
        TestConstants.AddBearerToken(request2, _tokenManager, userId: user.Id.ToString());
        TestConstants.AddIdempotencyKey(request2, idmKey); // Тот же ключ

        // Act & Assert 1
        using var result = await client.SendAsync(request);

        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Пользователь и вправду обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == user.Id);
        Assert.Equal(newFirstname, userFromDbAfterUpdate.Firstname);
        Assert.Equal(newUsername, userFromDbAfterUpdate.Username);
        Assert.Equal(newLanguageCode, userFromDbAfterUpdate.LanguageCode);

        // Act & Assert 2
        using var result2 = await client.SendAsync(request2);

        Assert.NotNull(result2);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result2.StatusCode);
        Assert.Null(result2.Content.Headers.ContentType);

        Assert.Equal(result.Headers.Count(), result2.Headers.Count());

        // Пользователь неизменился
        var userFromDbAfterUpdate2 = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == user.Id);
        Assert.Equivalent(userFromDbAfterUpdate, userFromDbAfterUpdate2);
    }

    [Fact] // Первый запрос - 204 (почта подтвердилась), второй запрос с тем же IdempotencyKey - 204 (почта второй раз не подтверждается)
    public async Task Get_Confirmations_Email_WhenTwoRequestsWithSameIdempotencyKey_ReturnsNoContent()
    {
        // Arrange
        string newFirstname = "новоеИмя";
        string newUsername = "newusername";
        string newLanguageCode = "nn";

        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isEmailConfirm: false);

        // Добавляем токен подтверждения почты
        var confirmEmailRequest = await DI.CreateConfirmEmailRequestAsync(_db, user.Id);
        string emailToken = confirmEmailRequest.Token;

        // Данные
        var data = new UpdateUserDto()
        {
            Firstname = newFirstname,
            Username = newUsername,
            LanguageCode = newLanguageCode
        };

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Get, string.Format(TestConstants.CONFIRMATIONS_EMAIL_TOKEN_URL, emailToken));
        var idmKey = TestConstants.AddIdempotencyKey(request);

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Get, string.Format(TestConstants.CONFIRMATIONS_EMAIL_TOKEN_URL, emailToken));
        TestConstants.AddIdempotencyKey(request2, idmKey); // Тот же ключ

        // Act & Assert 1
        using var result = await client.SendAsync(request);

        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Пользователь и вправду подтвердил почту
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == user.Id);
        Assert.True(userFromDbAfterUpdate.IsEmailConfirm);

        // Act & Assert 2
        using var result2 = await client.SendAsync(request2);

        Assert.NotNull(result2);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result2.StatusCode);
        Assert.Null(result2.Content.Headers.ContentType);

        Assert.Equal(result.Headers.Count(), result2.Headers.Count());

        // Пользователь неизменился
        var userFromDbAfterUpdate2 = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == user.Id);
        Assert.Equivalent(userFromDbAfterUpdate, userFromDbAfterUpdate2);

        // Запрос на подтверждение почты удалился из базы
        var requestFromDbAfterConfirm = await _db.ConfirmEmailRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == confirmEmailRequest.Id);
        Assert.Null(requestFromDbAfterConfirm);
    }

    [Fact] // Если IdempotencyKey не указан в заголовках или строке запроса
    public async Task Put_User_WhenIdempotencyKeyNotDefine_ReturnsBadRequest()
    {
        // Arrange
        string newFirstname = "новоеИмя";
        string newUsername = "newusername";
        string newLanguageCode = "nn";

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

        // Act
        using var result = await client.SendAsync(request);

        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Missing Idempotency-Key header or idmkey query string.", jsonDocument.RootElement.GetProperty("title").GetString());

        // Пользователь и вправду не обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == user.Id);
        Assert.NotEqual(newFirstname, userFromDbAfterUpdate.Firstname);
        Assert.NotEqual(newUsername, userFromDbAfterUpdate.Username);
        Assert.NotEqual(newLanguageCode, userFromDbAfterUpdate.LanguageCode);
    }

    [Fact] // Если IdempotencyKey указан в заголовках, но не удалось спарсить
    public async Task Put_User_WhenIdempotencyKeyDefineHeaderButNotParsing_ReturnsBadRequest()
    {
        // Arrange
        string newFirstname = "новоеИмя";
        string newUsername = "newusername";
        string newLanguageCode = "nn";
        string idmKey = "NOT PARSING GUID";

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
        TestConstants.AddIdempotencyKey(request, idmKey);

        // Act
        using var result = await client.SendAsync(request);

        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Invalid Idempotency-Key header.", jsonDocument.RootElement.GetProperty("title").GetString());

        // Пользователь и вправду не обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == user.Id);
        Assert.NotEqual(newFirstname, userFromDbAfterUpdate.Firstname);
        Assert.NotEqual(newUsername, userFromDbAfterUpdate.Username);
        Assert.NotEqual(newLanguageCode, userFromDbAfterUpdate.LanguageCode);
    }

    [Fact] // Если IdempotencyKey указан в строке запроса, но не удалось спарсить
    public async Task Get_Confirmations_Email_WhenIdempotencyKeyDefineQueryButNotParsing_ReturnsBadRequest()
    {
        // Arrange
        string newFirstname = "новоеИмя";
        string newUsername = "newusername";
        string newLanguageCode = "nn";
        string emailToken = "sometoken";
        string idmKey = "NOT PARSING GUID";

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
        var request = new HttpRequestMessage(HttpMethod.Get, string.Format(TestConstants.CONFIRMATIONS_EMAIL_TOKEN_URL, emailToken));
        TestConstants.AddIdempotencyKeyQuery(request, idmKey);

        // Act
        using var result = await client.SendAsync(request);

        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Invalid idmkey query string.", jsonDocument.RootElement.GetProperty("title").GetString());

        // Пользователь и вправду не обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == user.Id);
        Assert.NotEqual(newFirstname, userFromDbAfterUpdate.Firstname);
        Assert.NotEqual(newUsername, userFromDbAfterUpdate.Username);
        Assert.NotEqual(newLanguageCode, userFromDbAfterUpdate.LanguageCode);
    }

    [Fact] // Если IdempotencyKey указан в строке запроса, но это не GET запрос (я разрешаю использовать IdempotencyKey в строке запроса только для GET запросов)
    public async Task Put_User_WhenIdempotencyKeyDefineQueryButThisNotGetMethod_ReturnsBadRequest()
    {
        // Arrange
        string newFirstname = "новоеИмя";
        string newUsername = "newusername";
        string newLanguageCode = "nn";

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
        TestConstants.AddIdempotencyKeyQuery(request);

        // Act
        using var result = await client.SendAsync(request);

        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal($"The idmkey query string can only be used in {HttpMethods.Get} methods.", jsonDocument.RootElement.GetProperty("title").GetString());

        // Пользователь и вправду не обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == user.Id);
        Assert.NotEqual(newFirstname, userFromDbAfterUpdate.Firstname);
        Assert.NotEqual(newUsername, userFromDbAfterUpdate.Username);
        Assert.NotEqual(newLanguageCode, userFromDbAfterUpdate.LanguageCode);
    }

    [Fact] // Если один и тот же IdempotencyKey используется с разными телами
    public async Task Put_User_WhenSameIdempotencyKeyUsedDifferentBodies_ReturnsBadRequest()
    {
        // Arrange
        string newFirstname = "новоеИмя";
        string newUsername = "newusername";
        string newLanguageCode = "nn";

        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Данные 1 запроса
        var data = new UpdateUserDto()
        {
            Firstname = newFirstname,
            Username = newUsername,
            LanguageCode = newLanguageCode
        };

        // Данные 2 запроса
        var data2 = new UpdateUserDto()
        {
            Firstname = newFirstname,
            Username = newUsername + "some",
            LanguageCode = newLanguageCode
        };

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Put, TestConstants.USER_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());
        var idmKey = TestConstants.AddIdempotencyKey(request);

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Put, TestConstants.USER_URL);
        var json2 = new StringContent(JsonSerializer.Serialize(data2), Encoding.UTF8, Application.Json);
        request2.Content = json2;
        TestConstants.AddBearerToken(request2, _tokenManager, userId: user.Id.ToString());
        TestConstants.AddIdempotencyKey(request2, idmKey); // Тот же ключ

        // Act & Assert 1
        using var result = await client.SendAsync(request);

        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Пользователь и вправду обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == user.Id);
        Assert.Equal(newFirstname, userFromDbAfterUpdate.Firstname);
        Assert.Equal(newUsername, userFromDbAfterUpdate.Username);
        Assert.Equal(newLanguageCode, userFromDbAfterUpdate.LanguageCode);

        // Act & Assert 2
        using var result2 = await client.SendAsync(request2);

        Assert.NotNull(result2);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result2.StatusCode);
        Assert.Equal("application/problem+json", result2.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result2.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("This Idempotency-Key has already been used with another request data.", jsonDocument.RootElement.GetProperty("title").GetString());

        // Пользователь неизменился
        var userFromDbAfterUpdate2 = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == user.Id);
        Assert.Equivalent(userFromDbAfterUpdate, userFromDbAfterUpdate2);
    }

    [Fact] // Если один и тот же IdempotencyKey используется с разными заголовками, но одинаковыми телами
    public async Task Put_User_WhenSameIdempotencyKeyUsedDifferentHeaders_ReturnsBadRequest()
    {
        // Arrange
        string newFirstname = "новоеИмя";
        string newUsername = "newusername";
        string newLanguageCode = "nn";

        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Данные запроса
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
        var idmKey = TestConstants.AddIdempotencyKey(request);

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Put, TestConstants.USER_URL);
        var json2 = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request2.Content = json2;
        request2.Headers.Add("Something", "some"); // Заголовок, которого нет у первого запроса
        TestConstants.AddBearerToken(request2, _tokenManager, userId: user.Id.ToString());
        TestConstants.AddIdempotencyKey(request2, idmKey); // Тот же ключ

        // Act & Assert 1
        using var result = await client.SendAsync(request);

        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Пользователь и вправду обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == user.Id);
        Assert.Equal(newFirstname, userFromDbAfterUpdate.Firstname);
        Assert.Equal(newUsername, userFromDbAfterUpdate.Username);
        Assert.Equal(newLanguageCode, userFromDbAfterUpdate.LanguageCode);

        // Act & Assert 2
        using var result2 = await client.SendAsync(request2);

        Assert.NotNull(result2);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result2.StatusCode);
        Assert.Equal("application/problem+json", result2.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result2.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("This Idempotency-Key has already been used with another request data.", jsonDocument.RootElement.GetProperty("title").GetString());

        // Пользователь неизменился
        var userFromDbAfterUpdate2 = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == user.Id);
        Assert.Equivalent(userFromDbAfterUpdate, userFromDbAfterUpdate2);
    }
}