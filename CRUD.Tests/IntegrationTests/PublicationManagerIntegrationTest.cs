using Microsoft.AspNetCore.Mvc.Testing;

namespace CRUD.Tests.IntegrationTests;

[Collection(nameof(IntegrationsTestCollection))]
public sealed class PublicationManagerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IPublicationManager _publicationManager;
    private readonly ApplicationDbContext _db;

    public PublicationManagerIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _publicationManager = scopedServices.GetRequiredService<IPublicationManager>();
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
    }

    [Theory] // Корректные данные
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(25)]
    [InlineData(100)] // Если столько публикаций нет, то возвращаем сколько есть
    public async Task GetPublicationsDtoAsyncByCount_ReturnsIEnumerablePublicationDto(int count)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, null, ct: TestContext.Current.CancellationToken);

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
        }.Take(count);

        // Act
        var result = await _publicationManager.GetPublicationsDtoAsync(count, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        Assert.Equivalent(mustResult, result);

        // Проверяем, что AuthorFirstname не null
        foreach (var pub in result)
            Assert.NotNull(pub.AuthorFirstname);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(101)]
    public async Task GetPublicationsDtoAsyncByCount_NotValidData_ThrowsInvalidOperationException(int count)
    {
        // Arrange
        var getPublicationsDto = new GetPublicationsDto()
        {
            Count = count
        };
        var validatorsLocalizer = new ValidatorLocalizer();
        var validationResult = await new GetPublicationsDtoValidator(validatorsLocalizer).ValidateAsync(getPublicationsDto, TestContext.Current.CancellationToken);

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.GetPublicationsDtoAsync(count);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(GetPublicationsDto), validationResult.Errors), ex.Message);
    }

    [Theory] // Корректные данные, но публикаций нет вообще
    [InlineData(1)]
    [InlineData(25)]
    public async Task GetPublicationsDtoAsyncByCount_WhenPublicationsNotExists_ReturnsEmptyCollection(int count)
    {
        // Arrange

        // Act
        var result = await _publicationManager.GetPublicationsDtoAsync(count, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }


    [Fact] // Корректные данные
    public async Task GetPublicationsDtoAsyncByPageNumberAndPageSizeAndSearchStringAndSortBy_ReturnsPaginatedListDto()
    {
        // Arrange
        int pageIndex = 1;
        int pageSize = 2;

        // Добавляем автора в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);
        var publication3 = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);
        var publication4 = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);
        var publication5 = await DI.CreatePublicationAsync(_db, null, ct: TestContext.Current.CancellationToken);

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

        // Act
        var result = await _publicationManager.GetPublicationsDtoAsync(pageIndex, pageSize, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);

        Assert.Equivalent(mustResult, result);
    }

    [Theory]
    [InlineData("string1")]
    [InlineData("STRING1")]
    public async Task GetPublicationsDtoAsyncByPageNumberAndPageSizeAndSearchStringAndSortBy_SearchString_ReturnsPaginatedListDto(string searchString)
    {
        // Arrange
        int pageIndex = 1;
        int pageSize = 2;

        // Добавляем автора в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, title: searchString, ct: TestContext.Current.CancellationToken);
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);
        var publication3 = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);
        var publication4 = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);
        var publication5 = await DI.CreatePublicationAsync(_db, null, ct: TestContext.Current.CancellationToken);

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

        // Act
        var result = await _publicationManager.GetPublicationsDtoAsync(pageIndex, pageSize, searchString, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);

        Assert.Equivalent(mustResult, result);
    }

    [Fact]
    public async Task GetPublicationsDtoAsyncByPageNumberAndPageSizeAndSearchStringAndSortBy_SearchStringIsWhitespace_ReturnsPaginatedListDtoWithEmptyCollection()
    {
        // Arrange
        int pageIndex = 1;
        int pageSize = 2;
        string searchString = "   ";

        // Добавляем автора в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);
        var publication3 = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);
        var publication4 = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);
        var publication5 = await DI.CreatePublicationAsync(_db, null, ct: TestContext.Current.CancellationToken);

        // Такой результат должен быть
        var mustResult = new PaginatedListDto<PublicationDto>()
        {
            Items = [],
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = 0,
            SearchString = searchString,
            SortBy = SortByVariables.date,
            HasPreviousPage = false,
            HasNextPage = false
        };

        // Act
        var result = await _publicationManager.GetPublicationsDtoAsync(pageIndex, pageSize, searchString, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);

        Assert.Equivalent(mustResult, result);
    }

    [Fact]
    public async Task GetPublicationsDtoAsyncByPageNumberAndPageSizeAndSearchStringAndSortBy_SearchStringLengthNotValid_ReturnsPaginatedListDto()
    {
        // Arrange
        int pageIndex = 1;
        int pageSize = 2;
        int notValidLength = SearchStringValidator.MAX_LENGTH + 1;
        int validLength = SearchStringValidator.MAX_LENGTH;
        string searchString = new string('x', notValidLength); // 51 chars

        // Добавляем автора в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, content: searchString, ct: TestContext.Current.CancellationToken);
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);
        var publication3 = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);
        var publication4 = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);
        var publication5 = await DI.CreatePublicationAsync(_db, null, ct: TestContext.Current.CancellationToken);

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

        // Act
        var result = await _publicationManager.GetPublicationsDtoAsync(pageIndex, pageSize, searchString, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);

        Assert.Equivalent(mustResult, result);
    }

    [Fact]
    public async Task GetPublicationsDtoAsyncByPageNumberAndPageSizeAndSearchStringAndSortBy_SortBy_ReturnsPaginatedListDto()
    {
        // Arrange
        int pageIndex = 1;
        int pageSize = 2;
        string sortBy = SortByVariables.author_publications_count_desc;

        // Добавляем авторов в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var user2 = await DI.CreateUserAsync(_db, username: "test", email: "test@mail.ru", phoneNumber: "123456789", ct: TestContext.Current.CancellationToken);

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);
        var publication2 = await DI.CreatePublicationAsync(_db, user2.Id, ct: TestContext.Current.CancellationToken);
        var publication3 = await DI.CreatePublicationAsync(_db, user2.Id, ct: TestContext.Current.CancellationToken);
        var publication4 = await DI.CreatePublicationAsync(_db, user2.Id, ct: TestContext.Current.CancellationToken);
        var publication5 = await DI.CreatePublicationAsync(_db, null, ct: TestContext.Current.CancellationToken);

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

        // Act
        var result = await _publicationManager.GetPublicationsDtoAsync(pageIndex, pageSize, null, sortBy, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);

        Assert.Equivalent(mustResult, result);
    }

    [Theory]
    [InlineData(1, -1)]
    [InlineData(1, 0)]
    [InlineData(1, 26)]
    [InlineData(-1, 5)]
    public async Task GetPublicationsDtoAsyncByPageNumberAndPageSizeAndSearchStringAndSortBy_NotValidData_ThrowsInvalidOperationException(int pageIndex, int pageSize)
    {
        // Arrange
        var getPaginatedListDto = new GetPaginatedListDto()
        {
            PageIndex = pageIndex,
            PageSize = pageSize
        };
        var validatorsLocalizer = new ValidatorLocalizer();
        var validationResult = await new GetPaginatedListDtoValidator(validatorsLocalizer).ValidateAsync(getPaginatedListDto, TestContext.Current.CancellationToken);

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.GetPublicationsDtoAsync(pageIndex, pageSize);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(GetPaginatedListDto), validationResult.Errors), ex.Message);
    }

    [Fact] // Корректные данные, но публикаций нет вообще
    public async Task GetPublicationsDtoAsyncByPageNumberAndPageSizeAndSearchStringAndSortBy_WhenPublicationsNotExists_ReturnsPaginatedListDtoWithEmptyCollection()
    {
        // Arrange
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

        // Act
        var result = await _publicationManager.GetPublicationsDtoAsync(pageIndex, pageSize, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);

        Assert.Equivalent(mustResult, result);
    }


    [Theory] // Корректные данные
    [InlineData(1)]
    [InlineData(2)]
    public async Task GetAuthorsDtoAsync_ReturnsIEnumerableAuthorDto(int count)
    {
        // Arrange
        // Добавляем пользователей в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var user2 = await DI.CreateUserAsync(_db, username: "some", email: "some@some.ru", phoneNumber: "123456789", ct: TestContext.Current.CancellationToken);

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);
        var publication2 = await DI.CreatePublicationAsync(_db, null, ct: TestContext.Current.CancellationToken);
        var publication3 = await DI.CreatePublicationAsync(_db, user2.Id, ct: TestContext.Current.CancellationToken);
        var publication4 = await DI.CreatePublicationAsync(_db, user2.Id, ct: TestContext.Current.CancellationToken);

        // Такой результат должен быть
        var mustResult = new List<AuthorDto>()
        {
            new AuthorDto() { Firstname = user.Firstname, Username = user.Username, LanguageCode = user.LanguageCode, PublicationsCount = 1 },
            new AuthorDto() { Firstname = user2.Firstname, Username = user2.Username, LanguageCode = user2.LanguageCode, PublicationsCount = 2 },
        }.OrderBy(x => x.Username).Take(count); // В коде сортировка по Username

        // Act
        var result = await _publicationManager.GetAuthorsDtoAsync(count, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        Assert.Equivalent(mustResult, result);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(101)]
    public async Task GetAuthorsDtoAsync_NotValidData_ThrowsInvalidOperationException(int count)
    {
        // Arrange
        var getAuthorsDto = new GetAuthorsDto()
        {
            Count = count
        };
        var validatorsLocalizer = new ValidatorLocalizer();
        var validationResult = await new GetAuthorsDtoValidator(validatorsLocalizer).ValidateAsync(getAuthorsDto, TestContext.Current.CancellationToken);

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.GetAuthorsDtoAsync(count);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(GetAuthorsDto), validationResult.Errors), ex.Message);
    }

    [Fact] // Корректные данные, но авторов нет вообще
    public async Task GetAuthorsDtoAsync_WhenAuthorsNotExists_ReturnsEmptyCollection()
    {
        // Arrange
        int count = 1;

        // Act
        var result = await _publicationManager.GetAuthorsDtoAsync(count, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }


    [Fact] // У этого автора одна публикация и просим одну
    public async Task GetPublicationsDtoAsyncByCountAuthorId_WhenAuthorHaveOnePublication_ReturnsIEnumerablePublicationDto()
    {
        // Arrange
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

        // Act
        var result = await _publicationManager.GetPublicationsDtoAsync(count, authorIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value);

        Assert.Equivalent(mustResult, result.Value);
    }

    [Fact] // У этого автора две публикации и просим две
    public async Task GetPublicationsDtoAsyncByCountAuthorId_WhenAuthorHaveTwoPublication_ReturnsIEnumerablePublicationDto()
    {
        // Arrange
        int count = 2;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

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
        };

        // Act
        var result = await _publicationManager.GetPublicationsDtoAsync(count, authorIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value);

        Assert.Equivalent(mustResult, result.Value);
    }

    [Fact] // У этого автора две публикации и просим три, вернёт две
    public async Task GetPublicationsDtoAsyncByCountAuthorId_WhenAuthorHaveTwoPublication_ButAskThree_ReturnsIEnumerablePublicationDto()
    {
        // Arrange
        int count = 3;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

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
        };

        // Act
        var result = await _publicationManager.GetPublicationsDtoAsync(count, authorIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value);

        Assert.Equivalent(mustResult, result.Value);
    }

    [Fact] // У этого автора нет публикаций
    public async Task GetPublicationsDtoAsyncByCountAuthorId_WhenAuthorNotHavePublications_ReturnsIEnumerablePublicationDto()
    {
        // Arrange
        int count = 3;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var authorIdGuid = user.Id;

        // Такой результат должен быть
        var userFromDb = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == authorIdGuid, TestContext.Current.CancellationToken);
        var mustPublications = Enumerable.Empty<PublicationDto>();

        // Act
        var result = await _publicationManager.GetPublicationsDtoAsync(count, authorIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Value);

        Assert.Equivalent(mustPublications, result.Value);
    }

    [Theory]
    [InlineData(-1)] // Невалидное количество
    public async Task GetPublicationsDtoAsyncByCountAuthorId_NotValidData_ThrowsInvalidOperationException(int count)
    {
        // Arrange
        var getPublicationsDto = new GetPublicationsDto()
        {
            Count = count
        };
        var authorIdGuid = Guid.NewGuid();
        var validatorsLocalizer = new ValidatorLocalizer();
        var validationResult = await new GetPublicationsDtoValidator(validatorsLocalizer).ValidateAsync(getPublicationsDto, TestContext.Current.CancellationToken);

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.GetPublicationsDtoAsync(count, authorIdGuid);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(GetPublicationsDto), validationResult.Errors), ex.Message);
    }

    [Fact]
    public async Task GetPublicationsDtoAsyncByCountAuthorId_ReturnsErrorMessage_AuthorNotFound()
    {
        // Arrange
        int count = 1;
        var authorIdGuid = Guid.NewGuid();

        // Act
        var result = await _publicationManager.GetPublicationsDtoAsync(count, authorIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);

        Assert.Contains(ErrorMessages.AuthorNotFound, result.ErrorMessage);
    }

    [Fact] // Корректные данные, но публикаций нет вообще
    public async Task GetPublicationsDtoAsyncByCountAuthorId_ReturnsEmptyCollection()
    {
        // Arrange
        int count = 1;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var authorIdGuid = user.Id;

        // Act
        var result = await _publicationManager.GetPublicationsDtoAsync(count, authorIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }


    [Fact] // Корректные данные
    public async Task GetPublicationDtoAsync_ReturnsPublicationDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;

        // Такой результат должен быть
        var publicationFromDb = await _db.Publications.AsNoTracking().Include(x => x.User).FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        var mustPublicationDto = new PublicationDto
        {
            Id = publication.Id,
            CreatedAt = publication.CreatedAt.ToWithoutTicks(),
            EditedAt = publication.EditedAt?.ToWithoutTicks(),
            Title = publication.Title,
            Content = publication.Content,
            AuthorId = publication.AuthorId,
            AuthorFirstname = user.Firstname
        };

        // Act
        var result = await _publicationManager.GetPublicationDtoAsync(publicationIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Value);

        Assert.Equivalent(mustPublicationDto, result.Value);

        // Проверяем, что AuthorFirstname не null
        Assert.NotNull(result.Value.AuthorFirstname);
    }

    [Fact]
    public async Task GetPublicationDtoAsync_ReturnsErrorMessage_PublicationNotFound()
    {
        // Arrange
        var publicationIdGuid = Guid.NewGuid();

        // Act
        var result = await _publicationManager.GetPublicationDtoAsync(publicationIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);
        Assert.Contains(ErrorMessages.PublicationNotFound, result.ErrorMessage);
    }

    [Fact] // У этой публикации несуществующий автор
    public async Task GetPublicationDtoAsync_WhenAuthorNotFound_ReturnsPublicationDto()
    {
        // Arrange
        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, null, ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;

        // Act
        var result = await _publicationManager.GetPublicationDtoAsync(publicationIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Value);

        Assert.Equal("Автор удалён", result.Value.AuthorFirstname);
    }


    [Fact] // Корректные данные
    public async Task GetPublicationFullDtoAsync_ReturnsPublicationDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;

        // Такой результат должен быть
        var publicationFromDb = await _db.Publications.AsNoTracking().Include(x => x.User).FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        var mustPublicationDto = publicationFromDb.ToPublicationFullDto(publicationFromDb.User);

        // Act
        var result = await _publicationManager.GetPublicationFullDtoAsync(publicationIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Value);

        Assert.Equivalent(mustPublicationDto, result.Value);

        // Проверяем, что Author не null
        Assert.NotNull(result.Value.Author);
    }

    [Fact]
    public async Task GetPublicationFullDtoAsync_ReturnsErrorMessage_PublicationNotFound()
    {
        // Arrange
        var publicationIdGuid = Guid.NewGuid();

        // Act
        var result = await _publicationManager.GetPublicationFullDtoAsync(publicationIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);
        Assert.Contains(ErrorMessages.PublicationNotFound, result.ErrorMessage);
    }

    [Fact] // У этой публикации несуществующий автор
    public async Task GetPublicationFullDtoAsync_WhenAuthorNotFound_ReturnsPublicationDto()
    {
        // Arrange
        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, null, ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;

        // Act
        var result = await _publicationManager.GetPublicationFullDtoAsync(publicationIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Value);

        Assert.Null(result.Value.Author);
    }


    [Fact] // У этого пользователя одна статья | Новое содержимое
    public async Task UpdatePublicationAsync_WhenUserHaveOnePublication_ReturnsServiceResult()
    {
        // Arrange
        string title = "Title";
        string content = "new" + TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, userIdGuid, title: title, ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;
        var updatePublicationDto = new UpdatePublicationDto()
        {
            PublicationId = publicationIdGuid,
            Title = title,
            Content = content
        };

        // Act
        var result = await _publicationManager.UpdatePublicationAsync(userIdGuid, updatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Публикация и вправду обновилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Equal(updatePublicationDto.Title, publicationFromDbAfterUpdate.Title);
        Assert.Equal(updatePublicationDto.Content, publicationFromDbAfterUpdate.Content);
        Assert.NotNull(publicationFromDbAfterUpdate.EditedAt);
    }

    [Fact] // У этого пользователя две статьи | Новый заголовок и содержимое
    public async Task UpdatePublicationAsync_WhenUserHaveTwoPublications_ReturnsServiceResult()
    {
        // Arrange
        string title = "Title";
        string content = "new" + TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, userIdGuid, title: "Title1", content: "Content1", ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, userIdGuid, title: "Title2", content: "Content2", ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;
        var updatePublicationDto = new UpdatePublicationDto()
        {
            PublicationId = publicationIdGuid,
            Title = title,
            Content = content
        };

        // Act
        var result = await _publicationManager.UpdatePublicationAsync(userIdGuid, updatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Публикация и вправду обновилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Equal(updatePublicationDto.Title, publicationFromDbAfterUpdate.Title);
        Assert.Equal(updatePublicationDto.Content, publicationFromDbAfterUpdate.Content);
        Assert.NotNull(publicationFromDbAfterUpdate.EditedAt);
    }

    [Fact] // Новый заголовок и содержимое с запрещённым тегом
    public async Task UpdatePublicationAsync_MaliciousCode_ReturnsServiceResult()
    {
        // Arrange
        string title = "HTML";
        string content = "<div>ВРЕДОНОСНЫЙ КОД</div>" + TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, userIdGuid, title: "Title1", content: "Content1", ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;
        var updatePublicationDto = new UpdatePublicationDto()
        {
            PublicationId = publicationIdGuid,
            Title = title,
            Content = content
        };

        // Act
        var result = await _publicationManager.UpdatePublicationAsync(userIdGuid, updatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Публикация и вправду обновилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Equal(updatePublicationDto.Title, publicationFromDbAfterUpdate.Title);
        Assert.Equal(updatePublicationDto.Content.Replace("<div>ВРЕДОНОСНЫЙ КОД</div>", ""), publicationFromDbAfterUpdate.Content);
        Assert.NotNull(publicationFromDbAfterUpdate.EditedAt);
    }

    [Fact] // Повторяющиеся символы пробела, отступы, вредоносный код заменятся
    public async Task UpdatePublicationAsync_ExtraSpacesAndNewLinesAndMaliciousCode_ReturnsServiceResult()
    {
        // Arrange
        string title = "Title";
        string content = " Какой-то  очень странный  контент  с очень\tнепонятными отступами и пробелами\r\nи можно  ещё добавить   HTML разметку.  Например, <b>я биг босс</b> ватафа <script>И ещё вредоносный  код</script>\r\n\r\nдвойной отступ\n\r\n\rтройной";
        string expectedContent = " Какой-то очень странный контент с очень непонятными отступами и пробелами\nи можно ещё добавить HTML разметку. Например, <b>я биг босс</b> ватафа \nдвойной отступ\nтройной";

        // Пробелы по боками решает кастомный TrimStringConverter

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, userIdGuid, title: "Title1", content: "Content1", ct: TestContext.Current.CancellationToken);
        var publicationIdGuid = publication.Id;

        var updatePublicationDto = new UpdatePublicationDto()
        {
            PublicationId = publicationIdGuid,
            Title = title,
            Content = content
        };

        // Act
        var result = await _publicationManager.UpdatePublicationAsync(userIdGuid, updatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Публикация и вправду создалась без лишних пробелов и вредоносного кода
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Equal(updatePublicationDto.Title, publicationFromDbAfterUpdate.Title);

        // Ожидаемый контент совпадает
        Assert.Equal(expectedContent, publicationFromDbAfterUpdate.Content);
    }

    [Theory]
    [InlineData("Title", "notvalidcontent")] // Невалидное содержание
    public async Task UpdatePublicationAsync_NotValidData_ThrowsInvalidOperationException(string title, string content)
    {
        // Arrange
        var publicationIdGuid = Guid.NewGuid();
        var updatePublicationDto = new UpdatePublicationDto()
        {
            PublicationId = publicationIdGuid,
            Title = title,
            Content = content
        };
        var userIdGuid = Guid.NewGuid();
        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        var validatorsLocalizer = new ValidatorLocalizer();
        var validationResult = await new UpdatePublicationDtoValidator(validatorsLocalizer).ValidateAsync(updatePublicationDto, TestContext.Current.CancellationToken);

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.UpdatePublicationAsync(userIdGuid, updatePublicationDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(UpdatePublicationDto), validationResult.Errors), ex.Message);
    }

    [Fact]
    public async Task UpdatePublicationAsync_ReturnsErrorMessage_AuthorNotFound()
    {
        // Arrange
        string title = "Title";
        string content = "new" + TestConstants.PublicationContent;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, null, ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;

        var updatePublicationDto = new UpdatePublicationDto()
        {
            PublicationId = publicationIdGuid,
            Title = title,
            Content = content
        };
        var userIdGuid = Guid.NewGuid();
        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);

        // Act
        var result = await _publicationManager.UpdatePublicationAsync(userIdGuid, updatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.AuthorNotFound, result.ErrorMessage);

        // Публикация и вправду не обновилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Equivalent(publicationFromDbBeforeUpdate, publicationFromDbAfterUpdate);
    }

    [Fact]
    public async Task UpdatePublicationAsync_ReturnsErrorMessage_PublicationNotFound()
    {
        // Arrange
        string title = "Title";
        string content = "new" + TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = Guid.NewGuid();
        var updatePublicationDto = new UpdatePublicationDto()
        {
            PublicationId = publicationIdGuid,
            Title = title,
            Content = content
        };
        var userIdGuid = user.Id;

        // Act
        var result = await _publicationManager.UpdatePublicationAsync(userIdGuid, updatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.PublicationNotFound, result.ErrorMessage);
    }

    [Fact]
    public async Task UpdatePublicationAsync_WhenUserNotHavePublications_ReturnsErrorMessage_UserIsNotAuthorOfThisPublication()
    {
        // Arrange
        string title = "Title";
        string content = "new" + TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, null, ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;
        var updatePublicationDto = new UpdatePublicationDto()
        {
            PublicationId = publicationIdGuid,
            Title = title,
            Content = content
        };
        var userIdGuid = user.Id;
        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);

        // Act
        var result = await _publicationManager.UpdatePublicationAsync(userIdGuid, updatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserIsNotAuthorOfThisPublication, result.ErrorMessage);

        // Публикация и вправду не обновилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Equivalent(publicationFromDbBeforeUpdate, publicationFromDbAfterUpdate);
    }

    [Fact]
    public async Task UpdatePublicationAsync_WhenUserIsNotAuthorOfThisPublication_ReturnsErrorMessage_UserIsNotAuthorOfThisPublication()
    {
        // Arrange
        string title = "Title";
        string content = "new" + TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken); // Автор публикации

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        // Добавляем пользователя в базу
        var user2 = await DI.CreateUserAsync(_db, email: "test", username: "test", phoneNumber: "123456789", ct: TestContext.Current.CancellationToken);
        var userIdGuid = user2.Id;

        var publicationIdGuid = publication.Id;
        var updatePublicationDto = new UpdatePublicationDto()
        {
            PublicationId = publicationIdGuid,
            Title = title,
            Content = content
        };
        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);

        // Act
        var result = await _publicationManager.UpdatePublicationAsync(userIdGuid, updatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserIsNotAuthorOfThisPublication, result.ErrorMessage);

        // Публикация и вправду не обновилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Equivalent(publicationFromDbBeforeUpdate, publicationFromDbAfterUpdate);
    }

    [Fact]
    public async Task UpdatePublicationAsync_ReturnsErrorMessage_NoChangesDetected()
    {
        // Arrange
        string title = "Title";
        string content = TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, title: title, content: content, ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;
        var updatePublicationDto = new UpdatePublicationDto()
        {
            PublicationId = publicationIdGuid,
            Title = title,
            Content = content
        };
        var userIdGuid = user.Id;
        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);

        // Act
        var result = await _publicationManager.UpdatePublicationAsync(userIdGuid, updatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.NoChangesDetected, result.ErrorMessage);

        // Публикация и вправду не обновилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Equivalent(publicationFromDbBeforeUpdate, publicationFromDbAfterUpdate);
    }

    [Fact]
    public async Task UpdatePublicationAsync_NullObjects_ReturnsErrorMessage_NoChangesDetected()
    {
        // Arrange
        string title = null;
        string content = null;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, title: "title", content: "content", ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;
        var updatePublicationDto = new UpdatePublicationDto()
        {
            PublicationId = publicationIdGuid,
            Title = title,
            Content = content
        };
        var userIdGuid = user.Id;
        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);

        // Act
        var result = await _publicationManager.UpdatePublicationAsync(userIdGuid, updatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.NoChangesDetected, result.ErrorMessage);

        // Публикация и вправду не обновилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Equivalent(publicationFromDbBeforeUpdate, publicationFromDbAfterUpdate);
    }


    [Fact] // У этого пользователя одна статья | Новое содержимое
    public async Task UpdatePublicationAsyncByUpdatePublicationFullDto_WhenUserHaveOnePublication_ReturnsServiceResult()
    {
        // Arrange
        string title = "Title";
        string content = "new" + TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, userIdGuid, title: title, ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;
        var updatePublicationDto = new UpdatePublicationFullDto()
        {
            Title = title,
            Content = content
        };

        // Act
        var result = await _publicationManager.UpdatePublicationAsync(publicationIdGuid, updatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Публикация и вправду обновилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Equal(updatePublicationDto.Title, publicationFromDbAfterUpdate.Title);
        Assert.Equal(updatePublicationDto.Content, publicationFromDbAfterUpdate.Content);
    }

    [Fact] // У этого пользователя две статьи | Новый заголовок и содержимое
    public async Task UpdatePublicationAsyncByUpdatePublicationFullDto_WhenUserHaveTwoPublications_ReturnsServiceResult()
    {
        // Arrange
        string title = "Title";
        string content = "new" + TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, userIdGuid, title: "Title1", content: "Content1", ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, userIdGuid, title: "Title2", content: "Content2", ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;
        var updatePublicationDto = new UpdatePublicationFullDto()
        {
            Title = title,
            Content = content
        };


        // Act
        var result = await _publicationManager.UpdatePublicationAsync(publicationIdGuid, updatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Публикация и вправду обновилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Equal(updatePublicationDto.Title, publicationFromDbAfterUpdate.Title);
        Assert.Equal(updatePublicationDto.Content, publicationFromDbAfterUpdate.Content);
    }

    [Fact] // Новый заголовок и содержимое с запрещённым тегом
    public async Task UpdatePublicationAsyncByUpdatePublicationFullDto_MaliciousCode_ReturnsServiceResult()
    {
        // Arrange
        string title = "HTML";
        string content = "<div>ВРЕДОНОСНЫЙ КОД</div>" + TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, userIdGuid, title: "Title1", content: "Content1", ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;
        var updatePublicationDto = new UpdatePublicationFullDto()
        {
            Title = title,
            Content = content
        };

        // Act
        var result = await _publicationManager.UpdatePublicationAsync(publicationIdGuid, updatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Публикация и вправду обновилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Equal(updatePublicationDto.Title, publicationFromDbAfterUpdate.Title);
        Assert.Equal(updatePublicationDto.Content.Replace("<div>ВРЕДОНОСНЫЙ КОД</div>", ""), publicationFromDbAfterUpdate.Content);
    }

    [Fact] // Повторяющиеся символы пробела, отступы, вредоносный код заменятся
    public async Task UpdatePublicationAsyncByUpdatePublicationFullDto_ExtraSpacesAndNewLinesAndMaliciousCode_ReturnsServiceResult()
    {
        // Arrange
        string title = "Title";
        string content = " Какой-то  очень странный  контент  с очень\tнепонятными отступами и пробелами\r\nи можно  ещё добавить   HTML разметку.  Например, <b>я биг босс</b> ватафа <script>И ещё вредоносный  код</script>\r\n\r\nдвойной отступ\n\r\n\rтройной";
        string expectedContent = " Какой-то очень странный контент с очень непонятными отступами и пробелами\nи можно ещё добавить HTML разметку. Например, <b>я биг босс</b> ватафа \nдвойной отступ\nтройной";

        // Пробелы по боками решает кастомный TrimStringConverter

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, userIdGuid, title: "Title1", content: "Content1", ct: TestContext.Current.CancellationToken);
        var publicationIdGuid = publication.Id;

        var updatePublicationDto = new UpdatePublicationFullDto()
        {
            Title = title,
            Content = content
        };

        // Act
        var result = await _publicationManager.UpdatePublicationAsync(publicationIdGuid, updatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Публикация и вправду создалась без лишних пробелов и вредоносного кода
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Equal(updatePublicationDto.Title, publicationFromDbAfterUpdate.Title);

        // Ожидаемый контент совпадает
        Assert.Equal(expectedContent, publicationFromDbAfterUpdate.Content);
    }

    [Fact] // Новая дата
    public async Task UpdatePublicationAsyncByUpdatePublicationFullDto_NewDate_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;
        var updatePublicationDto = new UpdatePublicationFullDto()
        {
            Title = publication.Title,
            Content = publication.Content,
            CreatedAt = "2025-12-08T20:25:25.111111Z"
        };

        // Act
        var result = await _publicationManager.UpdatePublicationAsync(publicationIdGuid, updatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Публикация и вправду обновилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Equal(updatePublicationDto.Title, publicationFromDbAfterUpdate.Title);
        Assert.Equal(updatePublicationDto.Content, publicationFromDbAfterUpdate.Content);
        Assert.Equal(updatePublicationDto.CreatedAt, publicationFromDbAfterUpdate.CreatedAt.ToString(DateTimeFormats.WithTicks));
    }

    [Fact] // Новая дата не указана
    public async Task UpdatePublicationAsyncByUpdatePublicationFullDto_WithoutNewDate_NoChangesDetected()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;
        var updatePublicationDto = new UpdatePublicationFullDto()
        {
            Title = publication.Title,
            Content = publication.Content
        };

        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);

        // Act
        var result = await _publicationManager.UpdatePublicationAsync(publicationIdGuid, updatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.NoChangesDetected, result.ErrorMessage);

        // Публикация и вправду не обновилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Equivalent(publicationFromDbBeforeUpdate, publicationFromDbAfterUpdate);
    }

    [Theory]
    [InlineData("Title", "notvalidcontent", "2025")] // Невалидное содержание и дата
    public async Task UpdatePublicationAsyncByUpdatePublicationFullDto_NotValidData_ThrowsInvalidOperationException(string title, string content, string date)
    {
        // Arrange
        var publicationIdGuid = Guid.NewGuid();
        var updatePublicationDto = new UpdatePublicationFullDto()
        {
            Title = title,
            Content = content,
            CreatedAt = date
        };
        var userIdGuid = Guid.NewGuid();
        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        var validatorsLocalizer = new ValidatorLocalizer();
        var validationResult = await new UpdatePublicationFullDtoValidator(validatorsLocalizer).ValidateAsync(updatePublicationDto, TestContext.Current.CancellationToken);

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.UpdatePublicationAsync(publicationIdGuid, updatePublicationDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(UpdatePublicationFullDto), validationResult.Errors), ex.Message);
        Assert.Equal(2, validationResult.Errors.Count);
    }

    [Fact]
    public async Task UpdatePublicationAsyncByUpdatePublicationFullDto_ReturnsErrorMessage_PublicationNotFound()
    {
        // Arrange
        string title = "Title";
        string content = "new" + TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = Guid.NewGuid();
        var updatePublicationDto = new UpdatePublicationFullDto()
        {
            Title = title,
            Content = content
        };

        // Act
        var result = await _publicationManager.UpdatePublicationAsync(publicationIdGuid, updatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.PublicationNotFound, result.ErrorMessage);
    }

    [Fact]
    public async Task UpdatePublicationAsyncByUpdatePublicationFullDto_ReturnsErrorMessage_NoChangesDetected()
    {
        // Arrange
        string title = "Title";
        string content = TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, title: title, content: content, ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;
        var updatePublicationDto = new UpdatePublicationFullDto()
        {
            Title = title,
            Content = content
        };
        var userIdGuid = user.Id;
        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);

        // Act
        var result = await _publicationManager.UpdatePublicationAsync(publicationIdGuid, updatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.NoChangesDetected, result.ErrorMessage);

        // Публикация и вправду не обновилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Equivalent(publicationFromDbBeforeUpdate, publicationFromDbAfterUpdate);
    }

    [Fact]
    public async Task UpdatePublicationAsyncByUpdatePublicationFullDto_NullObjects_ReturnsErrorMessage_NoChangesDetected()
    {
        // Arrange
        string title = null;
        string content = null;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, title: "title", content: "content", ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;
        var updatePublicationDto = new UpdatePublicationFullDto()
        {
            Title = title,
            Content = content
        };
        var userIdGuid = user.Id;
        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);

        // Act
        var result = await _publicationManager.UpdatePublicationAsync(publicationIdGuid, updatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.NoChangesDetected, result.ErrorMessage);

        // Публикация и вправду не обновилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Equivalent(publicationFromDbBeforeUpdate, publicationFromDbAfterUpdate);
    }


    [Fact] // Корректные данные
    public async Task CreatePublicationAsync_ReturnsServiceResult()
    {
        // Arrange
        string title = "Title";
        string content = TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isEmailConfirm: true, isPhoneNumberConfirm: true, ct: TestContext.Current.CancellationToken);

        var createPublicationDto = new CreatePublicationDto()
        {
            Title = title,
            Content = content
        };
        var userIdGuid = user.Id;

        // Act
        var result = await _publicationManager.CreatePublicationAsync(userIdGuid, createPublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Публикация и вправду создалась | нахожу по автору и по DTO
        var publicationFromDbAfterCreate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.AuthorId == userIdGuid && x.Title == title && x.Content == content, TestContext.Current.CancellationToken);
        Assert.Equal(createPublicationDto.Title, publicationFromDbAfterCreate.Title);
        Assert.Equal(createPublicationDto.Content, publicationFromDbAfterCreate.Content);
    }

    [Fact] // Вредоносный код и вправду не попадает в базу
    public async Task CreatePublicationAsync_Malicious_Code_ReturnsServiceResult()
    {
        // Arrange
        string title = "Title";
        string maliciousCode = "<div>ВРЕДОНОСНЫЙ КОД</div>";
        string content = maliciousCode + TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isEmailConfirm: true, isPhoneNumberConfirm: true, ct: TestContext.Current.CancellationToken);

        var createPublicationDto = new CreatePublicationDto()
        {
            Title = title,
            Content = content
        };
        var userIdGuid = user.Id;

        // Act
        var result = await _publicationManager.CreatePublicationAsync(userIdGuid, createPublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Публикация и вправду создалась без вредоносного кода | нахожу по автору и по DTO
        var publicationFromDbAfterCreate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.AuthorId == userIdGuid && x.Title == title && x.Content == content.Replace(maliciousCode, ""), TestContext.Current.CancellationToken);
        Assert.Equal(createPublicationDto.Title, publicationFromDbAfterCreate.Title);
        Assert.Equal(createPublicationDto.Content.Replace(maliciousCode, ""), publicationFromDbAfterCreate.Content);
    }

    [Fact] // Повторяющиеся символы пробела, отступы, вредоносный код заменятся
    public async Task CreatePublicationAsync_ExtraSpacesAndNewLinesAndMaliciousCode_ReturnsServiceResult()
    {
        // Arrange
        string title = "Title";
        string content = " Какой-то  очень странный  контент  с очень\tнепонятными отступами и пробелами\r\nи можно  ещё добавить   HTML разметку.  Например, <b>я биг босс</b> ватафа <script>И ещё вредоносный  код</script>\r\n\r\nдвойной отступ\n\r\n\rтройной";
        string expectedContent = " Какой-то очень странный контент с очень непонятными отступами и пробелами\nи можно ещё добавить HTML разметку. Например, <b>я биг босс</b> ватафа \nдвойной отступ\nтройной";

        // Пробелы по боками решает кастомный TrimStringConverter

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isEmailConfirm: true, isPhoneNumberConfirm: true, ct: TestContext.Current.CancellationToken);

        var createPublicationDto = new CreatePublicationDto()
        {
            Title = title,
            Content = content
        };
        var userIdGuid = user.Id;

        // Act
        var result = await _publicationManager.CreatePublicationAsync(userIdGuid, createPublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Публикация и вправду создалась без лишних пробелов и вредоносного кода | нахожу по автору и по DTO
        var publicationFromDbAfterCreate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.AuthorId == userIdGuid && x.Title == title, TestContext.Current.CancellationToken);
        Assert.Equal(createPublicationDto.Title, publicationFromDbAfterCreate.Title);

        // Ожидаемый контент совпадает
        Assert.Equal(expectedContent, publicationFromDbAfterCreate.Content);
    }

    [Theory]
    [InlineData("Title", "content")] // Невалидное содержание
    [InlineData(null, null)] // Пустые данные
    public async Task CreatePublicationAsync_NotValidData_ThrowsInvalidOperationException(string title, string content)
    {
        // Arrange
        var createPublicationDto = new CreatePublicationDto()
        {
            Title = title,
            Content = content
        };
        var userIdGuid = Guid.NewGuid();
        var validatorsLocalizer = new ValidatorLocalizer();
        var validationResult = await new CreatePublicationDtoValidator(validatorsLocalizer).ValidateAsync(createPublicationDto, TestContext.Current.CancellationToken);

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.CreatePublicationAsync(userIdGuid, createPublicationDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(CreatePublicationDto), validationResult.Errors), ex.Message);
    }

    [Fact]
    public async Task CreatePublicationAsync_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        string title = "Title";
        string content = TestConstants.PublicationContent;

        var createPublicationDto = new CreatePublicationDto()
        {
            Title = title,
            Content = content
        };
        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _publicationManager.CreatePublicationAsync(userIdGuid, createPublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);

        // Публикация и вправду не создалась | нахожу по автору и по DTO
        var publicationFromDbAfterCreate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.AuthorId == userIdGuid && x.Title == title && x.Content == content, TestContext.Current.CancellationToken);
        Assert.Null(publicationFromDbAfterCreate);
    }

    [Fact]
    public async Task CreatePublicationAsync_ReturnsErrorMessage_UserHasNotConfirmedEmail()
    {
        // Arrange
        string title = "Title";
        string content = TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isEmailConfirm: false, isPhoneNumberConfirm: true, ct: TestContext.Current.CancellationToken);

        var createPublicationDto = new CreatePublicationDto()
        {
            Title = title,
            Content = content
        };
        var userIdGuid = user.Id;

        // Act
        var result = await _publicationManager.CreatePublicationAsync(userIdGuid, createPublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserHasNotConfirmedEmail, result.ErrorMessage);

        // Публикация и вправду не создалась | нахожу по автору и по DTO
        var publicationFromDbAfterCreate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.AuthorId == userIdGuid && x.Title == title && x.Content == content, TestContext.Current.CancellationToken);
        Assert.Null(publicationFromDbAfterCreate);
    }

    [Fact]
    public async Task CreatePublicationAsync_ReturnsErrorMessage_UserHasNotConfirmedPhoneNumber()
    {
        // Arrange
        string title = "Title";
        string content = TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isEmailConfirm: true, isPhoneNumberConfirm: false, ct: TestContext.Current.CancellationToken);

        var createPublicationDto = new CreatePublicationDto()
        {
            Title = title,
            Content = content
        };
        var userIdGuid = user.Id;

        // Act
        var result = await _publicationManager.CreatePublicationAsync(userIdGuid, createPublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserHasNotConfirmedPhoneNumber, result.ErrorMessage);

        // Публикация и вправду не создалась | нахожу по автору и по DTO
        var publicationFromDbAfterCreate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.AuthorId == userIdGuid && x.Title == title && x.Content == content, TestContext.Current.CancellationToken);
        Assert.Null(publicationFromDbAfterCreate);
    }


    [Fact] // Корректные данные
    public async Task DeletePublicationAsync_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;

        // Act
        var result = await _publicationManager.DeletePublicationAsync(userIdGuid, publicationIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Публикация и вправду удалилась
        var publicationFromDbAfterDelete = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Null(publicationFromDbAfterDelete);
    }

    [Fact]
    public async Task DeletePublicationAsync_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid(); // Пользователь, который пытается удалить

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken); // Автор публикации

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;
        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);

        // Act
        var result = await _publicationManager.DeletePublicationAsync(userIdGuid, publicationIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);

        // Публикация и вправду не удалилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Equivalent(publicationFromDbBeforeUpdate, publicationFromDbAfterUpdate);
    }

    [Fact]
    public async Task DeletePublicationAsync_PublicationNotFound()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        var publicationIdGuid = Guid.NewGuid();
        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);

        // Act
        var result = await _publicationManager.DeletePublicationAsync(userIdGuid, publicationIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.PublicationNotFound, result.ErrorMessage);

        // Публикация и вправду не удалилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Equivalent(publicationFromDbBeforeUpdate, publicationFromDbAfterUpdate);
    }

    [Fact]
    public async Task DeletePublicationAsync_ReturnsErrorMessage_UserIsNotAuthorOfThisPublication()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken); // Автор публикации

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        // Добавляем пользователя в базу
        var user2 = await DI.CreateUserAsync(_db, email: "test", username: "test", phoneNumber: "123456789", ct: TestContext.Current.CancellationToken);
        var userIdGuid = user2.Id;

        var publicationIdGuid = publication.Id;
        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);

        // Act
        var result = await _publicationManager.DeletePublicationAsync(userIdGuid, publicationIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserIsNotAuthorOfThisPublication, result.ErrorMessage);

        // Публикация и вправду не удалилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Equivalent(publicationFromDbBeforeUpdate, publicationFromDbAfterUpdate);
    }


    [Fact] // Корректные данные
    public async Task DeletePublicationAsyncByPublicationId_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;

        // Act
        var result = await _publicationManager.DeletePublicationAsync(publicationIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Публикация и вправду удалилась
        var publicationFromDbAfterDelete = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Null(publicationFromDbAfterDelete);
    }

    [Fact]
    public async Task DeletePublicationAsyncByPublicationId_PublicationNotFound()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        var publicationIdGuid = Guid.NewGuid();
        var publicationFromDbBeforeUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);

        // Act
        var result = await _publicationManager.DeletePublicationAsync(publicationIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.PublicationNotFound, result.ErrorMessage);

        // Публикация и вправду не удалилась
        var publicationFromDbAfterUpdate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);
        Assert.Equivalent(publicationFromDbBeforeUpdate, publicationFromDbAfterUpdate);
    }


    [Fact] // Корректные данные
    public async Task DeletePublicationsAsync_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);
        var publicationIdGuid = publication.Id;

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _publicationManager.DeletePublicationsAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Публикации и вправду удалились
        var publicationsFromDbAfterDelete = await _db.Publications.AsNoTracking().Where(x => x.AuthorId == userIdGuid).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Empty(publicationsFromDbAfterDelete);
    }

    [Fact]
    public async Task DeletePublicationsAsync_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _publicationManager.DeletePublicationsAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }


    [Fact]
    public async Task IsAuthorThisPublication_ReturnsTrue()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;

        // Act
        var result = await _publicationManager.IsAuthorThisPublicationAsync(userIdGuid, publicationIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsAuthorThisPublication_ReturnsFalse()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = Guid.NewGuid();

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;

        // Act
        var result = await _publicationManager.IsAuthorThisPublicationAsync(userIdGuid, publicationIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }
}