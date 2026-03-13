using CRUD.Models.Domains;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CRUD.Tests.SystemTests;

public class ClientApiSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;

    public ClientApiSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
    }

    [Fact]
    public async Task Post_Publications_ReturnsCreated()
    {
        // Arrange
        var client = _factory.HttpClient;

        string apiKey = TestConstants.UserApiKey;
        string title = "Title";
        string content = TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: true, isEmailConfirm: true, isPhoneNumberConfirm: true, apiKey: apiKey);

        var data = new ClientApiCreatePublicationDto()
        {
            ApiKey = apiKey,
            Title = title,
            Content = content
        };
        var userIdGuid = user.Id;

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.CLIENT_API_PUBLICATIONS_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Created, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(result.Headers.Location);

        // Публикация и вправду создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);
        Assert.Null(publicationFromDbBeforeCreatePublication);
        Assert.NotNull(publicationFromDbAfterCreatePublication);
        Assert.Equivalent(data.Title, publicationFromDbAfterCreatePublication.Title);
        Assert.Equivalent(data.Content, publicationFromDbAfterCreatePublication.Content);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<PublicationDto>();

        var expectedDto = new PublicationDto
        {
            Id = publicationFromDbAfterCreatePublication.Id,
            CreatedAt = publicationFromDbAfterCreatePublication.CreatedAt.ToWithoutTicks(),
            EditedAt = null,
            Title = publicationFromDbAfterCreatePublication.Title,
            Content = publicationFromDbAfterCreatePublication.Content,
            AuthorId = publicationFromDbAfterCreatePublication.AuthorId,
            AuthorFirstname = user.Firstname
        };

        // В ответе корректный PublicationDto
        Assert.Equivalent(expectedDto, response);
    }

    [Fact]
    public async Task Post_Publications_ReturnsInvalidApiKey()
    {
        // Arrange
        var client = _factory.HttpClient;

        string apiKey = TestConstants.UserApiKey;
        string title = "Title";
        string content = TestConstants.PublicationContent;

        var data = new ClientApiCreatePublicationDto()
        {
            ApiKey = apiKey,
            Title = title,
            Content = content
        };

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.CLIENT_API_PUBLICATIONS_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.INVALID_API_KEY, jsonDocument.RootElement.GetProperty("code").GetString());

        // Публикация и вправду не создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);
        Assert.Null(publicationFromDbBeforeCreatePublication);
        Assert.Null(publicationFromDbAfterCreatePublication);
    }

    [Fact]
    public async Task Post_Publications_ReturnsUserDoesNotHavePremium()
    {
        // Arrange
        var client = _factory.HttpClient;

        string apiKey = TestConstants.UserApiKey;
        string title = "Title";
        string content = TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: false, isEmailConfirm: true, isPhoneNumberConfirm: true, apiKey: apiKey);

        var data = new ClientApiCreatePublicationDto()
        {
            ApiKey = apiKey,
            Title = title,
            Content = content
        };

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.CLIENT_API_PUBLICATIONS_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.USER_DOES_NOT_HAVE_PREMIUM, jsonDocument.RootElement.GetProperty("code").GetString());

        // Публикация и вправду не создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);
        Assert.Null(publicationFromDbBeforeCreatePublication);
        Assert.Null(publicationFromDbAfterCreatePublication);
    }

    [Fact]
    public async Task Post_Publications_ReturnsUserHasNotConfirmedEmail()
    {
        // Arrange
        var client = _factory.HttpClient;

        string apiKey = TestConstants.UserApiKey;
        string title = "Title";
        string content = TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: true, isEmailConfirm: false, isPhoneNumberConfirm: true, apiKey: apiKey);

        var data = new ClientApiCreatePublicationDto()
        {
            ApiKey = apiKey,
            Title = title,
            Content = content
        };

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.CLIENT_API_PUBLICATIONS_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.USER_HAS_NOT_CONFIRMED_EMAIL, jsonDocument.RootElement.GetProperty("code").GetString());

        // Публикация и вправду не создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);
        Assert.Null(publicationFromDbBeforeCreatePublication);
        Assert.Null(publicationFromDbAfterCreatePublication);
    }

    [Fact]
    public async Task Post_Publications_ReturnsUserHasNotConfirmedPhoneNumber()
    {
        // Arrange
        var client = _factory.HttpClient;

        string apiKey = TestConstants.UserApiKey;
        string title = "Title";
        string content = TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: true, isEmailConfirm: true, isPhoneNumberConfirm: false, apiKey: apiKey);

        var data = new ClientApiCreatePublicationDto()
        {
            ApiKey = apiKey,
            Title = title,
            Content = content
        };

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.CLIENT_API_PUBLICATIONS_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.USER_HAS_NOT_CONFIRMED_PHONE_NUMBER, jsonDocument.RootElement.GetProperty("code").GetString());

        // Публикация и вправду не создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);
        Assert.Null(publicationFromDbBeforeCreatePublication);
        Assert.Null(publicationFromDbAfterCreatePublication);
    }


    // Конфликты параллельности


    [Fact]
    public async Task Post_Publications_ConcurrencyConflict_ReturnsCreatedOrConflictOrInvalidApiKey()
    {
        // Arrange
        var client = _factory.HttpClient;
        var client2 = _factory.CreateClient();

        string apiKey = TestConstants.UserApiKey;
        string title = "Title";
        string content = TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: true, isEmailConfirm: true, isPhoneNumberConfirm: true, apiKey: apiKey);

        var data = new ClientApiCreatePublicationDto()
        {
            ApiKey = apiKey,
            Title = title,
            Content = content
        };
        var userIdGuid = user.Id;

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.CLIENT_API_PUBLICATIONS_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Post, TestConstants.CLIENT_API_PUBLICATIONS_URL);
        var json2 = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request2.Content = json2;

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
            if (System.Net.HttpStatusCode.Created == result.StatusCode)
            {
                Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);
                Assert.NotNull(result.Headers.Location);

                // Публикация и вправду создалась
                var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);
                Assert.Null(publicationFromDbBeforeCreatePublication);
                Assert.NotNull(publicationFromDbAfterCreatePublication);
                Assert.Equivalent(data.Title, publicationFromDbAfterCreatePublication.Title);
                Assert.Equivalent(data.Content, publicationFromDbAfterCreatePublication.Content);

                continue;
            }

            // Читаем содержимое ответа
            await using var contentStream = await result.Content.ReadAsStreamAsync();
            using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

            // Может быть неуспешный ответ
            if (!result.IsSuccessStatusCode)
            {
                // Либо неверный API-ключ, либо Conflict
                var errorCode = jsonDocument.RootElement.GetProperty("code").GetString();
                string[] allowedErrors =
                [
                    ErrorCodes.INVALID_API_KEY,
                    ErrorCodes.CONCURRENCY_CONFLICTS
                ];

                Assert.Contains(errorCode, allowedErrors);
            }
        }
    }
}