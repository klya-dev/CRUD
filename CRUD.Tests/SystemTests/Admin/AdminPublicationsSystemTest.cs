using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CRUD.Tests.SystemTests.Admin;

public class AdminPublicationsSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;

    public AdminPublicationsSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
    }

    [Fact]
    public async Task Get_PublicationId_ReturnsPublicationDto()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id); // Дата с 7 знаками после запятой
        var publicationCreatedAt = (await _db.Publications.AsNoTracking().FirstAsync(x => x.Id == publication.Id)).CreatedAt; // Дата с 6 знаками после запятой (7ой - это 0), т.к из базы
        var expectedDto = new PublicationFullDto()
        {
            Id = publication.Id,
            Title = publication.Title,
            Content = publication.Content,
            CreatedAt = publicationCreatedAt,
            EditedAt = publication.EditedAt,
            Author = new UserFullDto
            {
                Id = user.Id,
                Firstname = user.Firstname,
                Username = user.Username,
                LanguageCode = user.LanguageCode,
                Role = user.Role,
                IsPremium = user.IsPremium,
                ApiKey = user.ApiKey,
                DisposableApiKey = user.DisposableApiKey,
                AvatarURL = user.AvatarURL,
                Email = user.Email,
                IsEmailConfirm = user.IsEmailConfirm,
                PhoneNumber = user.PhoneNumber,
                IsPhoneNumberConfirm = user.IsPhoneNumberConfirm
            }
        };

        // Запрос
        var url = string.Format(TestConstants.ADMIN_PUBLICATIONS_PUBLICATION_ID_URL, publication.Id);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<PublicationFullDto>();

        Assert.NotNull(response);
        Assert.NotNull(response.Title);
        Assert.NotNull(response.Content);
        Assert.NotNull(response.Author);

        Assert.Equivalent(expectedDto, response);
    }

    [Fact]
    public async Task Get_PublicationId_ReturnsPublicationNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Запрос
        var url = string.Format(TestConstants.ADMIN_PUBLICATIONS_PUBLICATION_ID_URL, Guid.NewGuid());
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.PUBLICATION_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_PublicationId_WhenAuthorNotFound_ReturnsPublicationDto()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, null);

        // Запрос
        var url = string.Format(TestConstants.ADMIN_PUBLICATIONS_PUBLICATION_ID_URL, publication.Id);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<PublicationFullDto>();

        Assert.NotNull(response);
        Assert.NotNull(response.Title);
        Assert.NotNull(response.Content);
        Assert.Null(response.Author);
    }


    [Fact] // У этого пользователя одна статья | Новое содержимое
    public async Task Patch_WhenUserHaveOnePublication_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.HttpClient;

        string title = "Title";
        string content = "new" + TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var userIdGuid = user.Id;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, userIdGuid, title: title);

        var publicationIdGuid = publication.Id;
        var data = new UpdatePublicationFullDto()
        {
            Title = title,
            Content = content
        };

        // Запрос
        var url = string.Format(TestConstants.ADMIN_PUBLICATIONS_PUBLICATION_ID_URL, publication.Id);
        var request = new HttpRequestMessage(HttpMethod.Patch, url);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Публикация и вправду обновилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid);
        Assert.Equal(data.Title, publicationFromDbAfterUpdate.Title);
        Assert.Equal(data.Content, publicationFromDbAfterUpdate.Content);
    }

    [Fact] // Новая дата
    public async Task Patch_NewDate_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, userIdGuid);
        var publicationIdGuid = publication.Id;

        var data = new UpdatePublicationFullDto()
        {
            Title = publication.Title,
            Content = publication.Content,
            CreatedAt = "2025-12-08T20:25:25.111111Z"
        };

        // Запрос
        var url = string.Format(TestConstants.ADMIN_PUBLICATIONS_PUBLICATION_ID_URL, publication.Id);
        var request = new HttpRequestMessage(HttpMethod.Patch, url);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Публикация и вправду обновилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid);
        Assert.Equal(data.Title, publicationFromDbAfterUpdate.Title);
        Assert.Equal(data.Content, publicationFromDbAfterUpdate.Content);
        Assert.Equal(data.CreatedAt, publicationFromDbAfterUpdate.CreatedAt.ToString(DateTimeFormats.WithTicks));
    }

    [Fact] // Новая дата не указана
    public async Task Patch_WithoutNewDate_ReturnsNoChangesDetected()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, userIdGuid);
        var publicationIdGuid = publication.Id;

        var data = new UpdatePublicationFullDto()
        {
            Title = publication.Title,
            Content = publication.Content
        };

        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid);

        // Запрос
        var url = string.Format(TestConstants.ADMIN_PUBLICATIONS_PUBLICATION_ID_URL, publication.Id);
        var request = new HttpRequestMessage(HttpMethod.Patch, url);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

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

        // Публикация и вправду не обновилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid);
        Assert.Equivalent(publicationFromDbBeforeUpdate, publicationFromDbAfterUpdate);
    }

    [Fact]
    public async Task Patch_ReturnsPublicationNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        string title = "Title";
        string content = "new" + TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var publicationIdGuid = Guid.NewGuid();
        var data = new UpdatePublicationFullDto()
        {
            Title = title,
            Content = content
        };

        // Запрос
        var url = string.Format(TestConstants.ADMIN_PUBLICATIONS_PUBLICATION_ID_URL, publicationIdGuid);
        var request = new HttpRequestMessage(HttpMethod.Patch, url);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.PUBLICATION_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Patch_ReturnsNoChangesDetected()
    {
        // Arrange
        var client = _factory.HttpClient;

        string title = "Title";
        string content = "new" + TestConstants.PublicationContent;

        // Добавляем по
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, title: title, content: content);

        var publicationIdGuid = publication.Id;
        var data = new UpdatePublicationFullDto()
        {
            Title = title,
            Content = content
        };
        var userIdGuid = user.Id;
        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid);

        // Запрос
        var url = string.Format(TestConstants.ADMIN_PUBLICATIONS_PUBLICATION_ID_URL, publication.Id);
        var request = new HttpRequestMessage(HttpMethod.Patch, url);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

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

        // Публикация и вправду не обновилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid);
        Assert.Equivalent(publicationFromDbBeforeUpdate, publicationFromDbAfterUpdate);
    }


    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, userIdGuid);
        var publicationIdGuid = publication.Id;

        // Запрос
        var url = string.Format(TestConstants.ADMIN_PUBLICATIONS_PUBLICATION_ID_URL, publicationIdGuid);
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);
        TestConstants.AddIdempotencyKey(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Публикация и вправду удалилась
        var publicationFromDbAfterDelete = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid);
        Assert.Null(publicationFromDbAfterDelete);
    }

    [Fact]
    public async Task Delete_ReturnsPublicationNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var publicationIdGuid = Guid.NewGuid();

        // Запрос
        var url = string.Format(TestConstants.ADMIN_PUBLICATIONS_PUBLICATION_ID_URL, publicationIdGuid);
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);
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

        Assert.Equal(ErrorCodes.PUBLICATION_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    [Fact]
    public async Task Delete_Authors_UserId_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, userIdGuid);
        var publicationIdGuid = publication.Id;

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, userIdGuid);

        // Запрос
        var url = string.Format(TestConstants.ADMIN_PUBLICATIONS_AUTHORS_USER_ID_URL, userIdGuid);
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Публикации и вправду удалились
        var publicationsFromDbAfterDelete = await _db.Publications.AsNoTracking().Where(x => x.AuthorId == userIdGuid).ToListAsync();
        Assert.Empty(publicationsFromDbAfterDelete);
    }

    [Fact]
    public async Task Delete_Authors_UserId_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        var userIdGuid = Guid.NewGuid();

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db); // Автор публикации

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        // Запрос
        var url = string.Format(TestConstants.ADMIN_PUBLICATIONS_AUTHORS_USER_ID_URL, userIdGuid);
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

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
}