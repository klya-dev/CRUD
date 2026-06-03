using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CRUD.Tests.SystemTests.User;

public sealed class UserPublicationsSystemTest : IClassFixture<TestWebApplicationFactory>
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
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

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
        using var result = await client.SendAsync(request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);
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
        var publication = await DI.CreatePublicationAsync(_db, null, ct: TestContext.Current.CancellationToken);

        // Запрос
        var url = $"{TestConstants.USER_PUBLICATIONS_URL}?count=" + count;
        var request = new HttpRequestMessage(HttpMethod.Get, url);
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

        Assert.Equal(ErrorCodes.AUTHOR_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_WhenPublicationsNotExists_ReturnsEmptyCollection()
    {
        // Arrange
        var client = _factory.HttpClient;

        int count = 2;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Запрос
        var url = $"{TestConstants.USER_PUBLICATIONS_URL}?count=" + count;
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);
        var response = jsonDocument.RootElement.Deserialize<IEnumerable<PublicationDto>>();

        Assert.NotNull(response);
        Assert.Empty(response);
    }
}