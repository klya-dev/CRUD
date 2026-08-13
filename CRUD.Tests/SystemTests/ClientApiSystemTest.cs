using CRUD.Models.Domains;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CRUD.Tests.SystemTests;

[Collection(nameof(IntegrationsTestCollection))]
public sealed class ClientApiSystemTest : IClassFixture<TestWebApplicationFactory>
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
        var user = await DI.CreateUserAsync(_db, isPremium: true, isEmailConfirm: true, isPhoneNumberConfirm: true, apiKey: apiKey, ct: TestContext.Current.CancellationToken);

        var data = new ClientApiCreatePublicationDto()
        {
            ApiKey = apiKey,
            Title = title,
            Content = content
        };
        var userIdGuid = user.Id;

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.CLIENT_API_PUBLICATIONS_URL)
        {
            Content = JsonContent.Create(data)
        };

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Created, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(result.Headers.Location);

        // Публикация и вправду создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);
        Assert.Null(publicationFromDbBeforeCreatePublication);
        Assert.NotNull(publicationFromDbAfterCreatePublication);
        Assert.Equivalent(data.Title, publicationFromDbAfterCreatePublication.Title);
        Assert.Equivalent(data.Content, publicationFromDbAfterCreatePublication.Content);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);
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
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.CLIENT_API_PUBLICATIONS_URL)
        {
            Content = JsonContent.Create(data)
        };

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.INVALID_API_KEY, jsonDocument.RootElement.GetProperty("code").GetString());

        // Публикация и вправду не создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);
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
        var user = await DI.CreateUserAsync(_db, isPremium: false, isEmailConfirm: true, isPhoneNumberConfirm: true, apiKey: apiKey, ct: TestContext.Current.CancellationToken);

        var data = new ClientApiCreatePublicationDto()
        {
            ApiKey = apiKey,
            Title = title,
            Content = content
        };

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.CLIENT_API_PUBLICATIONS_URL)
        {
            Content = JsonContent.Create(data)
        };

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.USER_DOES_NOT_HAVE_PREMIUM, jsonDocument.RootElement.GetProperty("code").GetString());

        // Публикация и вправду не создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);
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
        var user = await DI.CreateUserAsync(_db, isPremium: true, isEmailConfirm: false, isPhoneNumberConfirm: true, apiKey: apiKey, ct: TestContext.Current.CancellationToken);

        var data = new ClientApiCreatePublicationDto()
        {
            ApiKey = apiKey,
            Title = title,
            Content = content
        };

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.CLIENT_API_PUBLICATIONS_URL)
        {
            Content = JsonContent.Create(data)
        };

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.USER_HAS_NOT_CONFIRMED_EMAIL, jsonDocument.RootElement.GetProperty("code").GetString());

        // Публикация и вправду не создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);
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
        var user = await DI.CreateUserAsync(_db, isPremium: true, isEmailConfirm: true, isPhoneNumberConfirm: false, apiKey: apiKey, ct: TestContext.Current.CancellationToken);

        var data = new ClientApiCreatePublicationDto()
        {
            ApiKey = apiKey,
            Title = title,
            Content = content
        };

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.CLIENT_API_PUBLICATIONS_URL)
        {
            Content = JsonContent.Create(data)
        };

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.USER_HAS_NOT_CONFIRMED_PHONE_NUMBER, jsonDocument.RootElement.GetProperty("code").GetString());

        // Публикация и вправду не создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);
        Assert.Null(publicationFromDbBeforeCreatePublication);
        Assert.Null(publicationFromDbAfterCreatePublication);
    }
}