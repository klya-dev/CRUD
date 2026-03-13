using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CRUD.Tests.SystemTests.User;

public class UserPublicationsSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;

    public UserPublicationsSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
    }

    [Fact]
    public async Task Get_ReturnsIEnumerablePublicationDto()
    {
        // Arrange
        var client = _factory.HttpClient;

        int count = 1;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        var authorIdGuid = user.Id;

        // Такой результат должен быть
        var mustResult = new List<PublicationDto>()
        {
            new PublicationDto
            {
                Id = publication.Id,
                CreatedAt = publication.CreatedAt.ToWithoutTicks(),
                EditedAt = publication.EditedAt?.ToWithoutTicks(),
                Title = publication.Title,
                Content = publication.Content,
                AuthorId = publication.AuthorId,
                AuthorFirstname = user.Firstname
            }
        };

        // Запрос
        var url = $"{TestConstants.USER_PUBLICATIONS_URL}?count=" + count;
        var request = new HttpRequestMessage(HttpMethod.Get, url);
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
        var response = jsonDocument.RootElement.Deserialize<IEnumerable<PublicationDto>>();

        foreach (var item in response)
        {
            Assert.NotNull(item);
            Assert.NotNull(item.Title);
            Assert.NotNull(item.Content);
        }

        Assert.Equivalent(mustResult, response);
    }

    [Fact]
    public async Task Get_ReturnsAuthorNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        int count = 1;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, null);

        // Запрос
        var url = $"{TestConstants.USER_PUBLICATIONS_URL}?count=" + count;
        var request = new HttpRequestMessage(HttpMethod.Get, url);
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

        Assert.Equal(ErrorCodes.AUTHOR_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_WhenPublicationsNotExists_ReturnsEmptyCollection()
    {
        // Arrange
        var client = _factory.HttpClient;

        int count = 2;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Запрос
        var url = $"{TestConstants.USER_PUBLICATIONS_URL}?count=" + count;
        var request = new HttpRequestMessage(HttpMethod.Get, url);
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
        var response = jsonDocument.RootElement.Deserialize<IEnumerable<PublicationDto>>();

        Assert.NotNull(response);
        Assert.Empty(response);
    }


    // Конфликты параллельности


    [Fact]
    public async Task Get_ConcurrencyConflict_ReturnsIEnumerablePublicationDto()
    {
        // Arrange
        var client = _factory.HttpClient;
        var client2 = _factory.CreateClient();

        int count = 1;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        var authorIdGuid = user.Id;

        // Такой результат должен быть
        var mustResult = new List<PublicationDto>()
        {
            new PublicationDto
            {
                Id = publication.Id,
                CreatedAt = publication.CreatedAt.ToWithoutTicks(),
                EditedAt = publication.EditedAt?.ToWithoutTicks(),
                Title = publication.Title,
                Content = publication.Content,
                AuthorId = publication.AuthorId,
                AuthorFirstname = user.Firstname
            }
        };

        // Данные для запросов
        var url = $"{TestConstants.USER_PUBLICATIONS_URL}?count=" + count;

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Get, url);
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
            var response = jsonDocument.RootElement.Deserialize<IEnumerable<PublicationDto>>();

            foreach (var item in response)
            {
                Assert.NotNull(item);
                Assert.NotNull(item.Title);
                Assert.NotNull(item.Content);
            }

            Assert.Equivalent(mustResult, response);
        }
    }
}