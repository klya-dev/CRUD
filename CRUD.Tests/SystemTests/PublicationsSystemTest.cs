using CRUD.Models.Domains;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CRUD.Tests.SystemTests;

public class PublicationsSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;

    public PublicationsSystemTest(TestWebApplicationFactory factory)
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

        int count = 2;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, null);

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
            },
            new PublicationDto
            {
                Id = publication2.Id,
                CreatedAt = publication2.CreatedAt.ToWithoutTicks(),
                EditedAt = publication2.EditedAt?.ToWithoutTicks(),
                Title = publication2.Title,
                Content = publication2.Content,
                AuthorId = publication2.AuthorId,
                AuthorFirstname = "Автор удалён"
            }
        };

        // Запрос
        var url = $"{TestConstants.PUBLICATIONS_URL}?count={count}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<IEnumerable<PublicationDto>>();

        foreach (var item in response)
        {
            Assert.NotNull(item);
            Assert.NotNull(item.Title);
            Assert.NotNull(item.Content);
            Assert.NotNull(item.AuthorFirstname);
        }

        Assert.Equivalent(mustResult, response);
    }

    [Fact]
    public async Task Get_Age_ReturnsIEnumerablePublicationDto()
    {
        // Arrange
        var client = _factory.HttpClient;

        int count = 2;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, null);

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
            },
            new PublicationDto
            {
                Id = publication2.Id,
                CreatedAt = publication2.CreatedAt.ToWithoutTicks(),
                EditedAt = publication2.EditedAt?.ToWithoutTicks(),
                Title = publication2.Title,
                Content = publication2.Content,
                AuthorId = publication2.AuthorId,
                AuthorFirstname = "Автор удалён"
            }
        };

        // Запрос 1
        var url = $"{TestConstants.PUBLICATIONS_URL}?count={count}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        await client.SendAsync(request); // Отправляем запрос

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(result.Headers.Age);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<IEnumerable<PublicationDto>>();

        foreach (var item in response)
        {
            Assert.NotNull(item);
            Assert.NotNull(item.Title);
            Assert.NotNull(item.Content);
            Assert.NotNull(item.AuthorFirstname);
        }

        Assert.Equivalent(mustResult, response);
    }

    [Fact]
    public async Task Get_WhenPublicationsNotExists_ReturnsEmptyCollection()
    {
        // Arrange
        var client = _factory.HttpClient;

        int count = 2;

        // Запрос
        var url = $"{TestConstants.PUBLICATIONS_URL}?count={count}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<IEnumerable<PublicationDto>>();

        Assert.NotNull(response);
        Assert.Empty(response);
    }


    [Fact]
    public async Task Get_Paginated_ReturnsPaginatedListDto()
    {
        // Arrange
        var client = _factory.HttpClient;

        int pageIndex = 1;
        int pageSize = 2;

        // Добавляем автора в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication3 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication4 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication5 = await DI.CreatePublicationAsync(_db, null);

        // Такой результат должен быть
        var mustResult = new PaginatedListDto<PublicationDto>()
        {
            Items =
            [
                new PublicationDto
                {
                    Id = publication.Id,
                    CreatedAt = publication.CreatedAt.ToWithoutTicks(),
                    EditedAt = publication.EditedAt?.ToWithoutTicks(),
                    Title = publication.Title,
                    Content = publication.Content,
                    AuthorId = publication.AuthorId,
                    AuthorFirstname = user.Firstname
                },
                new PublicationDto
                {
                    Id = publication2.Id,
                    CreatedAt = publication2.CreatedAt.ToWithoutTicks(),
                    EditedAt = publication2.EditedAt?.ToWithoutTicks(),
                    Title = publication2.Title,
                    Content = publication2.Content,
                    AuthorId = publication2.AuthorId,
                    AuthorFirstname = user.Firstname
                }
            ],
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = 3,
            SearchString = null,
            SortBy = SortByVariables.date,
            HasPreviousPage = false,
            HasNextPage = true
        };

        // Запрос
        var url = $"{TestConstants.PUBLICATIONS_PAGINATED_URL}?pageIndex={pageIndex}&pageSize={pageSize}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<PaginatedListDto<PublicationDto>>();

        Assert.Equivalent(mustResult, response);
    }

    [Theory]
    [InlineData("string1")]
    [InlineData("STRING1")]
    public async Task Get_Paginated_SearchString_ReturnsPaginatedListDto(string searchString)
    {
        // Arrange
        var client = _factory.HttpClient;

        int pageIndex = 1;
        int pageSize = 2;

        // Добавляем автора в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, title: searchString);
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication3 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication4 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication5 = await DI.CreatePublicationAsync(_db, null);

        // Такой результат должен быть
        var mustResult = new PaginatedListDto<PublicationDto>()
        {
            Items =
            [
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
            ],
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = 1,
            SearchString = searchString,
            SortBy = SortByVariables.date,
            HasPreviousPage = false,
            HasNextPage = false
        };

        // Запрос
        var url = $"{TestConstants.PUBLICATIONS_PAGINATED_URL}?pageIndex={pageIndex}&pageSize={pageSize}&searchString={searchString}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<PaginatedListDto<PublicationDto>>();

        Assert.Equivalent(mustResult, response);
    }

    [Fact]
    public async Task Get_Paginated_SearchStringIsWhitespace_ReturnsPaginatedListDtoWithEmptyCollection()
    {
        // Arrange
        var client = _factory.HttpClient;

        int pageIndex = 1;
        int pageSize = 2;
        string searchString = "%20%20%20";

        // Добавляем автора в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication3 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication4 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication5 = await DI.CreatePublicationAsync(_db, null);

        // Такой результат должен быть
        var mustResult = new PaginatedListDto<PublicationDto>()
        {
            Items = [],
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = 0,
            SearchString = "   ",
            SortBy = SortByVariables.date,
            HasPreviousPage = false,
            HasNextPage = false
        };

        // Запрос
        var url = $"{TestConstants.PUBLICATIONS_PAGINATED_URL}?pageIndex={pageIndex}&pageSize={pageSize}&searchString={searchString}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<PaginatedListDto<PublicationDto>>();

        Assert.Equivalent(mustResult, response);
    }

    [Fact]
    public async Task Get_Paginated_SearchStringLengthNotValid_ReturnsPaginatedListDtoWithEmptyCollection()
    {
        // Arrange
        var client = _factory.HttpClient;

        int pageIndex = 1;
        int pageSize = 2;
        int notValidLength = 51;
        int validLength = 50;
        string searchString = new string('x', notValidLength); // 51 chars

        // Добавляем автора в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, content: searchString);
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication3 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication4 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication5 = await DI.CreatePublicationAsync(_db, null);

        // Такой результат должен быть
        var mustResult = new PaginatedListDto<PublicationDto>()
        {
            Items =
            [
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
            ],
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = 1,
            SearchString = searchString.Remove(validLength, 1),
            SortBy = SortByVariables.date,
            HasPreviousPage = false,
            HasNextPage = false
        };

        // Запрос
        var url = $"{TestConstants.PUBLICATIONS_PAGINATED_URL}?pageIndex={pageIndex}&pageSize={pageSize}&searchString={searchString}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<PaginatedListDto<PublicationDto>>();

        Assert.Equivalent(mustResult, response);
    }

    [Fact]
    public async Task Get_Paginated_SortBy_ReturnsPaginatedListDto()
    {
        // Arrange
        var client = _factory.HttpClient;

        int pageIndex = 1;
        int pageSize = 2;
        string sortBy = SortByVariables.author_publications_count_desc;

        // Добавляем авторов в базу
        var user = await DI.CreateUserAsync(_db);
        var user2 = await DI.CreateUserAsync(_db, username: "test", email: "test@mail.ru", phoneNumber: "123456789");

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);
        var publication2 = await DI.CreatePublicationAsync(_db, user2.Id);
        var publication3 = await DI.CreatePublicationAsync(_db, user2.Id);
        var publication4 = await DI.CreatePublicationAsync(_db, user2.Id);
        var publication5 = await DI.CreatePublicationAsync(_db, null);

        // Такой результат должен быть
        var mustResult = new PaginatedListDto<PublicationDto>()
        {
            Items =
            [
                new PublicationDto
                {
                    Id = publication2.Id,
                    CreatedAt = publication2.CreatedAt.ToWithoutTicks(),
                    EditedAt = publication2.EditedAt?.ToWithoutTicks(),
                    Title = publication2.Title,
                    Content = publication2.Content,
                    AuthorId = publication2.AuthorId,
                    AuthorFirstname = user2.Firstname
                },
                new PublicationDto
                {
                    Id = publication3.Id,
                    CreatedAt = publication3.CreatedAt.ToWithoutTicks(),
                    EditedAt = publication3.EditedAt?.ToWithoutTicks(),
                    Title = publication3.Title,
                    Content = publication3.Content,
                    AuthorId = publication3.AuthorId,
                    AuthorFirstname = user2.Firstname
                }
            ],
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = 3,
            SearchString = null,
            SortBy = sortBy,
            HasPreviousPage = false,
            HasNextPage = true
        };

        // Запрос
        var url = $"{TestConstants.PUBLICATIONS_PAGINATED_URL}?pageIndex={pageIndex}&pageSize={pageSize}&sortBy={sortBy}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<PaginatedListDto<PublicationDto>>();

        Assert.Equivalent(mustResult, response);
    }

    [Fact]
    public async Task Get_Paginated_Age_ReturnsPaginatedListDto()
    {
        // Arrange
        var client = _factory.HttpClient;

        int pageIndex = 1;
        int pageSize = 2;

        // Добавляем автора в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication3 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication4 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication5 = await DI.CreatePublicationAsync(_db, null);

        // Такой результат должен быть
        var mustResult = new PaginatedListDto<PublicationDto>()
        {
            Items =
            [
                new PublicationDto
                {
                    Id = publication.Id,
                    CreatedAt = publication.CreatedAt.ToWithoutTicks(),
                    EditedAt = publication.EditedAt?.ToWithoutTicks(),
                    Title = publication.Title,
                    Content = publication.Content,
                    AuthorId = publication.AuthorId,
                    AuthorFirstname = user.Firstname
                },
                new PublicationDto
                {
                    Id = publication2.Id,
                    CreatedAt = publication2.CreatedAt.ToWithoutTicks(),
                    EditedAt = publication2.EditedAt?.ToWithoutTicks(),
                    Title = publication2.Title,
                    Content = publication2.Content,
                    AuthorId = publication2.AuthorId,
                    AuthorFirstname = user.Firstname
                }
            ],
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = 3,
            SearchString = null,
            SortBy = SortByVariables.date,
            HasPreviousPage = false,
            HasNextPage = true
        };

        // Запрос 1
        var url = $"{TestConstants.PUBLICATIONS_PAGINATED_URL}?pageIndex={pageIndex}&pageSize={pageSize}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        await client.SendAsync(request); // Отправляем запрос

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(result.Headers.Age);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<PaginatedListDto<PublicationDto>>();

        Assert.Equivalent(mustResult, response);
    }

    [Fact]
    public async Task Get_Paginated_WhenPublicationsNotExists_ReturnsPaginatedListDtoWithEmptyCollection()
    {
        // Arrange
        var client = _factory.HttpClient;

        int pageIndex = 1;
        int pageSize = 2;

        // Такой результат должен быть
        var mustResult = new PaginatedListDto<PublicationDto>()
        {
            Items = [],
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = 0,
            SearchString = null,
            SortBy = SortByVariables.date,
            HasPreviousPage = false,
            HasNextPage = false
        };

        // Запрос
        var url = $"{TestConstants.PUBLICATIONS_PAGINATED_URL}?pageIndex={pageIndex}&pageSize={pageSize}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<PaginatedListDto<PublicationDto>>();

        Assert.NotNull(response);
        Assert.Equivalent(mustResult, response);
    }


    [Fact]
    public async Task Get_Authors_ReturnsIEnumerableAuthorDto()
    {
        // Arrange
        var client = _factory.HttpClient;

        int count = 2;

        // Добавляем пользователей в базу
        var user = await DI.CreateUserAsync(_db);
        var user2 = await DI.CreateUserAsync(_db, username: "some", email: "some@some.ru", phoneNumber: "123456789");

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);
        var publication2 = await DI.CreatePublicationAsync(_db, null);
        var publication3 = await DI.CreatePublicationAsync(_db, user2.Id);
        var publication4 = await DI.CreatePublicationAsync(_db, user2.Id);

        // Такой результат должен быть
        var mustResult = new List<AuthorDto>()
        {
            new AuthorDto() { Firstname = user.Firstname, Username = user.Username, LanguageCode = user.LanguageCode, PublicationsCount = 1 },
            new AuthorDto() { Firstname = user2.Firstname, Username = user2.Username, LanguageCode = user2.LanguageCode, PublicationsCount = 2 },
        }.OrderBy(x => x.Username); // В коде сортировка по Username

        // Запрос
        var url = $"{TestConstants.PUBLICATIONS_AUTHORS_URL}?count={count}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<IEnumerable<AuthorDto>>();

        foreach (var item in response)
        {
            Assert.NotNull(item);
            Assert.NotNull(item.Firstname);
            Assert.NotNull(item.Username);
            Assert.NotNull(item.LanguageCode);
        }

        Assert.Equivalent(mustResult, response);
    }

    [Fact]
    public async Task Get_Authors_Age_ReturnsIEnumerablePublicationDto()
    {
        // Arrange
        var client = _factory.HttpClient;

        int count = 2;

        // Добавляем пользователей в базу
        var user = await DI.CreateUserAsync(_db);
        var user2 = await DI.CreateUserAsync(_db, username: "some", email: "some@some.ru", phoneNumber: "123456789");

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);
        var publication2 = await DI.CreatePublicationAsync(_db, null);
        var publication3 = await DI.CreatePublicationAsync(_db, user2.Id);
        var publication4 = await DI.CreatePublicationAsync(_db, user2.Id);

        // Такой результат должен быть
        var mustResult = new List<AuthorDto>()
        {
            new AuthorDto() { Firstname = user.Firstname, Username = user.Username, LanguageCode = user.LanguageCode, PublicationsCount = 1 },
            new AuthorDto() { Firstname = user2.Firstname, Username = user2.Username, LanguageCode = user2.LanguageCode, PublicationsCount = 2 },
        }.OrderBy(x => x.Username); // В коде сортировка по Username

        // Запрос 1
        var url = $"{TestConstants.PUBLICATIONS_AUTHORS_URL}?count={count}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        await client.SendAsync(request); // Отправляем запрос

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(result.Headers.Age);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<IEnumerable<AuthorDto>>();

        foreach (var item in response)
        {
            Assert.NotNull(item);
            Assert.NotNull(item.Firstname);
            Assert.NotNull(item.Username);
            Assert.NotNull(item.LanguageCode);
        }

        Assert.Equivalent(mustResult, response);
    }

    [Fact]
    public async Task Get_Authors_WhenPublicationsNotExists_ReturnsEmptyCollection()
    {
        // Arrange
        var client = _factory.HttpClient;

        int count = 2;

        // Добавляем пользователей в базу
        var user = await DI.CreateUserAsync(_db);

        // Запрос
        var url = $"{TestConstants.PUBLICATIONS_AUTHORS_URL}?count={count}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<IEnumerable<AuthorDto>>();

        Assert.NotNull(response);
        Assert.Empty(response);
    }

    [Fact]
    public async Task Get_Authors_WhenUsersNotExists_ReturnsEmptyCollection()
    {
        // Arrange
        var client = _factory.HttpClient;

        int count = 2;

        // Запрос
        var url = $"{TestConstants.PUBLICATIONS_AUTHORS_URL}?count={count}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<IEnumerable<AuthorDto>>();

        Assert.NotNull(response);
        Assert.Empty(response);
    }


    [Fact]
    public async Task Get_Authors_AuthorId_ReturnsIEnumerablePublicationDto()
    {
        // Arrange
        var client = _factory.HttpClient;

        int count = 1;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

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
        var url = string.Format(TestConstants.PUBLICATIONS_AUTHORS_AUTHOR_ID_URL, user.Id) + $"?count={count}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<IEnumerable<PublicationDto>>();

        foreach (var item in response)
        {
            Assert.NotNull(item);
            Assert.NotNull(item.Title);
            Assert.NotNull(item.Content);
        }

        Assert.Equivalent(mustResult, response);
    }

    [Fact]
    public async Task Get_Authors_AuthorId_ReturnsAuthorNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        int count = 1;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, null);

        // Запрос
        var url = string.Format(TestConstants.PUBLICATIONS_AUTHORS_AUTHOR_ID_URL, Guid.NewGuid()) + $"?count={count}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

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
    public async Task Get_Authors_AuthorId_WhenPublicationsNotExists_ReturnsEmptyCollection()
    {
        // Arrange
        var client = _factory.HttpClient;

        int count = 2;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Запрос
        var url = string.Format(TestConstants.PUBLICATIONS_AUTHORS_AUTHOR_ID_URL, user.Id) + $"?count={count}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<IEnumerable<PublicationDto>>();

        Assert.NotNull(response);
        Assert.Empty(response);
    }


    [Fact]
    public async Task Get_PublicationId_ReturnsPublicationDto()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        var publicationFromDb = await _db.Publications.AsNoTracking().Include(x => x.User).FirstOrDefaultAsync(x => x.Id == publication.Id);
        var expectedDto = new PublicationDto
        {
            Id = publicationFromDb.Id,
            CreatedAt = publicationFromDb.CreatedAt.ToWithoutTicks(),
            EditedAt = publicationFromDb.EditedAt?.ToWithoutTicks(),
            Title = publicationFromDb.Title,
            Content = publicationFromDb.Content,
            AuthorId = publicationFromDb.AuthorId,
            AuthorFirstname = user.Firstname
        };

        // Запрос
        var url = string.Format(TestConstants.PUBLICATIONS_PUBLICATION_ID_URL, publication.Id);
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<PublicationDto>();

        Assert.NotNull(response);
        Assert.NotNull(response.Title);
        Assert.NotNull(response.Content);
        Assert.NotNull(response.AuthorFirstname);

        Assert.Equivalent(expectedDto, response);
    }

    [Fact]
    public async Task Get_PublicationId_ReturnsPublicationNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Запрос
        var url = string.Format(TestConstants.PUBLICATIONS_PUBLICATION_ID_URL, Guid.NewGuid());
        var request = new HttpRequestMessage(HttpMethod.Get, url);

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
        var url = string.Format(TestConstants.PUBLICATIONS_PUBLICATION_ID_URL, publication.Id);
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<PublicationDto>();

        Assert.NotNull(response);
        Assert.NotNull(response.Title);
        Assert.NotNull(response.Content);

        Assert.Equal("Автор удалён", response.AuthorFirstname);
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
        var data = new UpdatePublicationDto()
        {
            PublicationId = publicationIdGuid,
            Title = title,
            Content = content
        };

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Patch, TestConstants.PUBLICATIONS_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

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

    [Fact]
    public async Task Patch_ReturnsAuthorNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        string title = "Title";
        string content = "new" + TestConstants.PublicationContent;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, null);

        var publicationIdGuid = publication.Id;

        var data = new UpdatePublicationDto()
        {
            PublicationId = publicationIdGuid,
            Title = title,
            Content = content
        };
        var userIdGuid = Guid.NewGuid();
        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Patch, TestConstants.PUBLICATIONS_URL);
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

        Assert.Equal(ErrorCodes.AUTHOR_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());

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
        var data = new UpdatePublicationDto()
        {
            PublicationId = publicationIdGuid,
            Title = title,
            Content = content
        };

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Patch, TestConstants.PUBLICATIONS_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

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
    public async Task Patch_ReturnsUserIsNotAuthorOfThisPublication()
    {
        // Arrange
        var client = _factory.HttpClient;

        string title = "Title";
        string content = "new" + TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db); // Автор публикации

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        // Добавляем пользователя в базу
        var user2 = await DI.CreateUserAsync(_db, email: "test", username: "test", phoneNumber: "123456789");

        var publicationIdGuid = publication.Id;
        var data = new UpdatePublicationDto()
        {
            PublicationId = publicationIdGuid,
            Title = title,
            Content = content
        };
        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Patch, TestConstants.PUBLICATIONS_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, userId: user2.Id.ToString());

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.USER_IS_NOT_AUTHOR_OF_THIS_PUBLICATION, jsonDocument.RootElement.GetProperty("code").GetString());

        // Публикация и вправду не обновилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid);
        Assert.Equivalent(publicationFromDbBeforeUpdate, publicationFromDbAfterUpdate);
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
        var data = new UpdatePublicationDto()
        {
            PublicationId = publicationIdGuid,
            Title = title,
            Content = content
        };
        var userIdGuid = user.Id;
        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Patch, TestConstants.PUBLICATIONS_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

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
    public async Task Post_ReturnsCreated()
    {
        // Arrange
        var client = _factory.HttpClient;

        string title = "Title";
        string content = TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isEmailConfirm: true, isPhoneNumberConfirm: true);

        var data = new CreatePublicationDto()
        {
            Title = title,
            Content = content
        };
        var userIdGuid = user.Id;

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.PUBLICATIONS_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Created, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(result.Headers.Location);

        // Публикация и вправду создалась | нахожу по автору и по DTO
        var publicationFromDbAfterCreate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.AuthorId == userIdGuid && x.Title == title && x.Content == content);
        Assert.Equal(data.Title, publicationFromDbAfterCreate.Title);
        Assert.Equal(data.Content, publicationFromDbAfterCreate.Content);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<PublicationDto>();

        var expectedDto = new PublicationDto
        {
            Id = publicationFromDbAfterCreate.Id,
            CreatedAt = publicationFromDbAfterCreate.CreatedAt.ToWithoutTicks(),
            EditedAt = null,
            Title = publicationFromDbAfterCreate.Title,
            Content = publicationFromDbAfterCreate.Content,
            AuthorId = publicationFromDbAfterCreate.AuthorId,
            AuthorFirstname = user.Firstname
        };

        // В ответе корректный PublicationDto
        Assert.Equivalent(expectedDto, response);
    }

    [Fact]
    public async Task Post_Malicious_Code_ReturnsCreated()
    {
        // Arrange
        var client = _factory.HttpClient;

        string title = "Title";
        string maliciousCode = "<div>ВРЕДОНОСНЫЙ КОД</div>";
        string content = maliciousCode + TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isEmailConfirm: true, isPhoneNumberConfirm: true);

        var data = new CreatePublicationDto()
        {
            Title = title,
            Content = content
        };
        var userIdGuid = user.Id;

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.PUBLICATIONS_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Created, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(result.Headers.Location);

        // Публикация и вправду создалась без вредоносного кода | нахожу по автору и по DTO
        var publicationFromDbAfterCreate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.AuthorId == userIdGuid && x.Title == title && x.Content == content.Replace(maliciousCode, ""));
        Assert.Equal(data.Title, publicationFromDbAfterCreate.Title);
        Assert.Equal(data.Content.Replace(maliciousCode, ""), publicationFromDbAfterCreate.Content);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<PublicationDto>();

        var expectedDto = new PublicationDto
        {
            Id = publicationFromDbAfterCreate.Id,
            CreatedAt = publicationFromDbAfterCreate.CreatedAt.ToWithoutTicks(),
            EditedAt = null,
            Title = publicationFromDbAfterCreate.Title,
            Content = publicationFromDbAfterCreate.Content,
            AuthorId = publicationFromDbAfterCreate.AuthorId,
            AuthorFirstname = user.Firstname
        };

        // В ответе корректный PublicationDto
        Assert.Equivalent(expectedDto, response);
    }

    [Fact]
    public async Task Post_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        string title = "Title";
        string content = TestConstants.PublicationContent;

        var data = new CreatePublicationDto()
        {
            Title = title,
            Content = content
        };
        var userIdGuid = Guid.NewGuid();

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.PUBLICATIONS_URL);
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

        var publicationFromDbAfterCreate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.AuthorId == userIdGuid && x.Title == title && x.Content == content);
        Assert.Null(publicationFromDbAfterCreate);
    }

    [Fact]
    public async Task Post_ReturnsUserHasNotConfirmedEmail()
    {
        // Arrange
        var client = _factory.HttpClient;

        string title = "Title";
        string content = TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isEmailConfirm: false, isPhoneNumberConfirm: true);

        var data = new CreatePublicationDto()
        {
            Title = title,
            Content = content
        };
        var userIdGuid = user.Id;

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.PUBLICATIONS_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, userId: userIdGuid.ToString());

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

        // Публикация и вправду не создалась | нахожу по автору и по DTO
        var publicationFromDbAfterCreate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.AuthorId == userIdGuid && x.Title == title && x.Content == content);
        Assert.Null(publicationFromDbAfterCreate);
    }

    [Fact]
    public async Task Post_ReturnsUserHasNotConfirmedPhoneNumber()
    {
        // Arrange
        var client = _factory.HttpClient;

        string title = "Title";
        string content = TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isEmailConfirm: true, isPhoneNumberConfirm: false);

        var data = new CreatePublicationDto()
        {
            Title = title,
            Content = content
        };
        var userIdGuid = user.Id;

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.PUBLICATIONS_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, userId: userIdGuid.ToString());

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

        // Публикация и вправду не создалась | нахожу по автору и по DTO
        var publicationFromDbAfterCreate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.AuthorId == userIdGuid && x.Title == title && x.Content == content);
        Assert.Null(publicationFromDbAfterCreate);
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
        var url = string.Format(TestConstants.PUBLICATIONS_PUBLICATION_ID_URL, publicationIdGuid);
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());
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
    public async Task Delete_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        var userIdGuid = Guid.NewGuid(); // Пользователь, который пытается удалить

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db); // Автор публикации

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);
        var publicationIdGuid = publication.Id;

        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid);

        // Запрос
        var url = string.Format(TestConstants.PUBLICATIONS_PUBLICATION_ID_URL, publicationIdGuid);
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        TestConstants.AddBearerToken(request, _tokenManager, userId: userIdGuid.ToString());
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

        // Публикация и вправду не удалилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid);
        Assert.Equivalent(publicationFromDbBeforeUpdate, publicationFromDbAfterUpdate);
    }

    [Fact]
    public async Task Delete_ReturnsPublicationNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        var publicationIdGuid = Guid.NewGuid();
        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid);

        // Запрос
        var url = string.Format(TestConstants.PUBLICATIONS_PUBLICATION_ID_URL, publicationIdGuid);
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        TestConstants.AddBearerToken(request, _tokenManager, userId: userIdGuid.ToString());
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

        // Публикация и вправду не удалилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid);
        Assert.Equivalent(publicationFromDbBeforeUpdate, publicationFromDbAfterUpdate);
    }

    [Fact]
    public async Task Delete_ReturnsUserIsNotAuthorOfThisPublication()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователей в базу
        var user = await DI.CreateUserAsync(_db); // Автор публикации
        var user2 = await DI.CreateUserAsync(_db, email: "test", username: "test", phoneNumber: "123456789");

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);
        var publicationIdGuid = publication.Id;

        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid);

        // Запрос
        var url = string.Format(TestConstants.PUBLICATIONS_PUBLICATION_ID_URL, publicationIdGuid);
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user2.Id.ToString());
        TestConstants.AddIdempotencyKey(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.USER_IS_NOT_AUTHOR_OF_THIS_PUBLICATION, jsonDocument.RootElement.GetProperty("code").GetString());

        // Публикация и вправду не удалилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid);
        Assert.Equivalent(publicationFromDbBeforeUpdate, publicationFromDbAfterUpdate);
    }


    // Конфликты параллельности


    [Fact]
    public async Task Get_ConcurrencyConflict_ReturnsIEnumerablePublicationDto()
    {
        // Arrange
        var client = _factory.HttpClient;
        var client2 = _factory.CreateClient();

        int count = 2;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, null);

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
            },
            new PublicationDto
            {
                Id = publication2.Id,
                CreatedAt = publication2.CreatedAt.ToWithoutTicks(),
                EditedAt = publication2.EditedAt?.ToWithoutTicks(),
                Title = publication2.Title,
                Content = publication2.Content,
                AuthorId = publication2.AuthorId,
                AuthorFirstname = "Автор удалён"
            }
        };

        // Данные для запросов
        var url = $"{TestConstants.PUBLICATIONS_URL}?count=" + count;

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Get, url);

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
            var response = jsonDocument.Deserialize<IEnumerable<PublicationDto>>();

            foreach (var item in response)
            {
                Assert.NotNull(item);
                Assert.NotNull(item.Title);
                Assert.NotNull(item.Content);
                Assert.NotNull(item.AuthorFirstname);
            }

            Assert.Equal(2, mustResult.Count);
            Assert.Equal(2, response.Count());
            Assert.Equivalent(mustResult, response);
        }
    }


    [Fact]
    public async Task Get_Authors_ConcurrencyConflict_ReturnsIEnumerableAuthorDto()
    {
        // Arrange
        var client = _factory.HttpClient;
        var client2 = _factory.CreateClient();

        int count = 2;

        // Добавляем пользователей в базу
        var user = await DI.CreateUserAsync(_db);
        var user2 = await DI.CreateUserAsync(_db, username: "some", email: "some@some.ru", phoneNumber: "123456789");

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);
        var publication2 = await DI.CreatePublicationAsync(_db, null);
        var publication3 = await DI.CreatePublicationAsync(_db, user2.Id);
        var publication4 = await DI.CreatePublicationAsync(_db, user2.Id);

        // Такой результат должен быть
        var mustResult = new List<AuthorDto>()
        {
            new AuthorDto() { Firstname = user.Firstname, Username = user.Username, LanguageCode = user.LanguageCode, PublicationsCount = 1 },
            new AuthorDto() { Firstname = user2.Firstname, Username = user2.Username, LanguageCode = user2.LanguageCode, PublicationsCount = 2 },
        }.OrderBy(x => x.Username); // В коде сортировка по Username

        // Данные для запросов
        var url = $"{TestConstants.PUBLICATIONS_AUTHORS_URL}?count={count}";

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Get, url);

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
            var response = jsonDocument.Deserialize<IEnumerable<AuthorDto>>();

            foreach (var item in response)
            {
                Assert.NotNull(item);
                Assert.NotNull(item.Firstname);
                Assert.NotNull(item.Username);
                Assert.NotNull(item.LanguageCode);
            }

            Assert.Equal(2, mustResult.Count());
            Assert.Equal(2, response.Count());
            Assert.Equivalent(mustResult, response);
        }
    }


    [Fact]
    public async Task Get_Authors_AuthorId_ConcurrencyConflict_ReturnsIEnumerablePublicationDto()
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
        var url = string.Format(TestConstants.PUBLICATIONS_AUTHORS_AUTHOR_ID_URL, authorIdGuid) + $"?count={count}";

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Get, url);

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
            var response = jsonDocument.Deserialize<IEnumerable<PublicationDto>>();

            foreach (var item in response)
            {
                Assert.NotNull(item);
                Assert.NotNull(item.Title);
                Assert.NotNull(item.Content);
            }

            Assert.Equivalent(mustResult, response);
        }
    }


    [Fact]
    public async Task Get_PublicationId_ConcurrencyConflict_ReturnsPublicationDto()
    {
        // Arrange
        var client = _factory.HttpClient;
        var client2 = _factory.CreateClient();

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        var publicationFromDb = await _db.Publications.AsNoTracking().Include(x => x.User).FirstOrDefaultAsync(x => x.Id == publication.Id);
        var expectedDto = new PublicationDto
        {
            Id = publicationFromDb.Id,
            CreatedAt = publicationFromDb.CreatedAt.ToWithoutTicks(),
            EditedAt = publicationFromDb.EditedAt?.ToWithoutTicks(),
            Title = publicationFromDb.Title,
            Content = publicationFromDb.Content,
            AuthorId = publicationFromDb.AuthorId,
            AuthorFirstname = user.Firstname
        };

        // Данные для запросов
        var url = string.Format(TestConstants.PUBLICATIONS_PUBLICATION_ID_URL, publication.Id);

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Get, url);

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
            var response = jsonDocument.Deserialize<PublicationDto>();

            Assert.NotNull(response);
            Assert.NotNull(response.Title);
            Assert.NotNull(response.Content);
            Assert.NotNull(response.AuthorFirstname);

            Assert.Equivalent(expectedDto, response);
        }
    }


    [Fact] // У этого пользователя одна статья | Новое содержимое
    public async Task Patch_ConcurrencyConflict_WhenUserHaveOnePublication_ReturnsNoContentOrConflictOrNoChangesDetected()
    {
        // Arrange
        var client = _factory.HttpClient;
        var client2 = _factory.CreateClient();

        string title = "Title";
        string content = "new" + TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, userIdGuid, title: title);
        var publicationIdGuid = publication.Id;

        // Данные для запросов
        var data = new UpdatePublicationDto()
        {
            PublicationId = publicationIdGuid,
            Title = title,
            Content = content
        };

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Patch, TestConstants.PUBLICATIONS_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Patch, TestConstants.PUBLICATIONS_URL);
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

                // Публикация и вправду обновилась
                var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid);
                Assert.Equal(data.Title, publicationFromDbAfterUpdate.Title);
                Assert.Equal(data.Content, publicationFromDbAfterUpdate.Content);

                continue;
            }

            // Читаем содержимое ответа
            await using var contentStream = await result.Content.ReadAsStreamAsync();
            using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

            // Может быть неуспешный ответ
            if (!result.IsSuccessStatusCode)
            {
                // Либо изменения не обнаружены, Conflict
                var errorCode = jsonDocument.RootElement.GetProperty("code").GetString();
                string[] allowedErrors =
                [
                    ErrorCodes.NO_CHANGES_DETECTED,
                    ErrorCodes.CONCURRENCY_CONFLICTS
                ];

                Assert.Contains(errorCode, allowedErrors);
            }
        }
    }


    [Fact]
    public async Task Post_ConcurrencyConflict_ReturnsCreatedOrConflict()
    {
        // Arrange
        var client = _factory.HttpClient;
        var client2 = _factory.CreateClient();

        string title = "Title";
        string content = TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isEmailConfirm: true, isPhoneNumberConfirm: true);

        var userIdGuid = user.Id;

        // Данные для запросов
        var data = new CreatePublicationDto()
        {
            Title = title,
            Content = content
        };

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.PUBLICATIONS_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Post, TestConstants.PUBLICATIONS_URL);
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
            if (System.Net.HttpStatusCode.Created == result.StatusCode)
            {
                Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);
                Assert.NotNull(result.Headers.Location);

                // Публикация и вправду создалась | нахожу по автору и по DTO
                var publicationFromDbAfterCreate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.AuthorId == userIdGuid && x.Title == title && x.Content == content);
                Assert.Equal(data.Title, publicationFromDbAfterCreate.Title);
                Assert.Equal(data.Content, publicationFromDbAfterCreate.Content);

                continue;
            }

            // Читаем содержимое ответа
            await using var contentStream = await result.Content.ReadAsStreamAsync();
            using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

            // Может быть неуспешный ответ
            if (!result.IsSuccessStatusCode)
            {
                // Conflict
                var errorCode = jsonDocument.RootElement.GetProperty("code").GetString();
                string[] allowedErrors =
                [
                    ErrorCodes.CONCURRENCY_CONFLICTS
                ];

                Assert.Contains(errorCode, allowedErrors);
            }
        }
    }


    [Fact]
    public async Task Delete_ConcurrencyConflict_ReturnsNoContentOrConflictOrPublicationNotFoundOrUserIsNotAuthorOfThisPublication()
    {
        // Arrange
        var client = _factory.HttpClient;
        var client2 = _factory.CreateClient();

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, userIdGuid);
        var publicationIdGuid = publication.Id;

        // Данные для запросов
        var url = string.Format(TestConstants.PUBLICATIONS_PUBLICATION_ID_URL, publicationIdGuid);

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());
        TestConstants.AddIdempotencyKey(request);

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Delete, url);
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

                // Публикация и вправду удалилась
                var publicationFromDbAfterDelete = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid);
                Assert.Null(publicationFromDbAfterDelete);

                continue;
            }

            // Читаем содержимое ответа
            await using var contentStream = await result.Content.ReadAsStreamAsync();
            using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

            // Может быть неуспешный ответ
            if (!result.IsSuccessStatusCode)
            {
                // Либо изменения не обнаружены, Conflict
                var errorCode = jsonDocument.RootElement.GetProperty("code").GetString();
                string[] allowedErrors =
                [
                    ErrorCodes.PUBLICATION_NOT_FOUND,
                    ErrorCodes.USER_IS_NOT_AUTHOR_OF_THIS_PUBLICATION,
                    ErrorCodes.CONCURRENCY_CONFLICTS
                ];

                Assert.Contains(errorCode, allowedErrors);
            }
        }
    }
}