#nullable disable
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace CRUD.Tests.UnitTests;

public class PublicationManagerUnitTest
{
    // #nullable disable

    private readonly ApplicationDbContext _db;

    private readonly Mock<IValidator<Publication>> _mockPublicationValidator;
    private readonly Mock<IValidator<GetPublicationsDto>> _mockGetPublicationsDtoValidator;
    private readonly Mock<IValidator<GetPaginatedListDto>> _mockGetPaginatedListDtoValidator;
    private readonly Mock<IValidator<GetAuthorsDto>> _mockGetAuthorsDtoValidator;
    private readonly Mock<IValidator<UpdatePublicationDto>> _mockUpdatePublicationDtoValidator;
    private readonly Mock<IValidator<UpdatePublicationFullDto>> _mockUpdatePublicationFullDtoValidator;
    private readonly Mock<IValidator<CreatePublicationDto>> _mockCreatePublicationDtoValidator;
    private readonly Mock<HtmlHelper> _mockHtmlHelper;
    private readonly Mock<HybridCache> _mockHybridCache;
    private readonly PublicationManager _publicationManager;

    public PublicationManagerUnitTest()
    {
        var db = DbContextGenerator.GenerateDbContextTestInMemory();
        _db = db;

        _mockPublicationValidator = new();
        _mockGetPublicationsDtoValidator = new();
        _mockGetPaginatedListDtoValidator = new();
        _mockGetAuthorsDtoValidator = new();
        _mockUpdatePublicationDtoValidator = new();
        _mockUpdatePublicationFullDtoValidator = new();
        _mockCreatePublicationDtoValidator = new();
        _mockHtmlHelper = new();
        _mockHybridCache = new();

        _publicationManager = new PublicationManager(
            db,
            _mockPublicationValidator.Object,
            _mockGetPublicationsDtoValidator.Object,
            _mockGetPaginatedListDtoValidator.Object,
            _mockGetAuthorsDtoValidator.Object,
            _mockUpdatePublicationDtoValidator.Object,
            _mockUpdatePublicationFullDtoValidator.Object,
            _mockCreatePublicationDtoValidator.Object,
            _mockHtmlHelper.Object,
            _mockHybridCache.Object
        );
    }


    [Fact]
    public async Task GetPublicationsDtoAsyncByPageNumberAndPageSizeAndSearchStringAndSortBy_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        int pageIndex = 1;
        int pageSize = 2;
        string sortBy = null;

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.GetPublicationsDtoAsync(pageIndex, pageSize, sortBy: sortBy);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(sortBy), ex.ParamName);
    }


    [Fact]
    public async Task GetPublicationsDtoAsyncByCountAuthorId_NotValidGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier()
    {
        // Arrange
        int count = 1;
        var authorIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.GetPublicationsDtoAsync(count, authorIdGuid);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }


    [Fact]
    public async Task GetPublicationDtoAsync_NotValidGuid_ThrowsInvalidOperationException()
    {
        // Arrange
        var publicationIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.GetPublicationDtoAsync(publicationIdGuid);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }


    [Fact]
    public async Task GetPublicationFullDtoAsync_NotValidGuid_ThrowsInvalidOperationException()
    {
        // Arrange
        var publicationIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.GetPublicationFullDtoAsync(publicationIdGuid);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }


    [Fact]
    public async Task UpdatePublicationAsync_NotValidGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier()
    {
        // Arrange
        Guid userId = Guid.Empty;
        string title = "title";
        string content = TestConstants.PublicationContent;

        var publicationIdGuid = Guid.NewGuid();

        var updatePublicationDto = new UpdatePublicationDto()
        {
            PublicationId = publicationIdGuid,
            Title = title,
            Content = content
        };

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.UpdatePublicationAsync(userId, updatePublicationDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }

    [Fact] // Перед записью в базу должно выбросится исключение, о том, что Publication невалидный
    public async Task UpdatePublicationAsync_ThrowsInvalidOperationException_NotValidBeforeUpdate()
    {
        // Arrange
        string title = "Title";
        string content = TestConstants.PublicationContent;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        var publicationIdGuid = publication.Id;
        var updatePublicationDto = new UpdatePublicationDto()
        {
            Title = title,
            Content = content,
            PublicationId = publicationIdGuid
        };
        var userIdGuid = user.Id;

        // Какие-то ошибки
        var validationResultPublication = new ValidationResult()
        {
            Errors =
            [
                new ValidationFailure("PropertyName", "ErrorMessage")
            ]
        };

        // Проходит валидацию
        _mockUpdatePublicationDtoValidator.Setup(x => x.ValidateAsync(updatePublicationDto, default)).ReturnsAsync(new ValidationResult());

        // Не проходит валидацию
        _mockPublicationValidator.Setup(x => x.ValidateAsync(It.IsAny<Publication>(), default)).ReturnsAsync(validationResultPublication); // Возвращаем ошибки валидации на любого Publication'а, который впихивается в ValidateAsync

        // Пользователь является автором публикации
        //_mockHybridCache.Setup(x => x.GetOrCreateAsync<bool>(It.IsAny<string>(), It.IsAny<Func<CancellationToken, ValueTask<bool>>>())).ReturnsAsync(false);
        //await _mockHybridCache.Object.SetAsync(
        //    $"{CacheKeys.IsAuthorThisPublication}-{userIdGuid}:{publicationIdGuid}", true);
        _mockHybridCache.Setup(x => x.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<string, CancellationToken, ValueTask<bool>>>(), It.IsAny<HybridCacheEntryOptions>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.UpdatePublicationAsync(userIdGuid, updatePublicationDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(Publication), validationResultPublication.Errors), ex.Message);
    }

    [Fact]
    public async Task UpdatePublicationAsync_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        UpdatePublicationDto updatePublicationDto = null;
        var userIdGuid = Guid.NewGuid();

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.UpdatePublicationAsync(userIdGuid, updatePublicationDto);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(updatePublicationDto), ex.ParamName);
    }


    [Fact]
    public async Task UpdatePublicationAsyncByUpdatePublicationFullDto_NotValidGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier()
    {
        // Arrange
        Guid userId = Guid.Empty;
        string title = "title";
        string content = TestConstants.PublicationContent;

        var publicationIdGuid = Guid.NewGuid();

        var updatePublicationDto = new UpdatePublicationFullDto()
        {
            Title = title,
            Content = content
        };

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.UpdatePublicationAsync(userId, updatePublicationDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }

    [Fact]
    public async Task UpdatePublicationAsyncByUpdatePublicationFullDto_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        UpdatePublicationFullDto updatePublicationFullDto = null;
        var userIdGuid = Guid.NewGuid();

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.UpdatePublicationAsync(userIdGuid, updatePublicationFullDto);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(updatePublicationFullDto), ex.ParamName);
    }


    [Fact]
    public async Task CreatePublicationAsync_NotValidGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier()
    {
        // Arrange
        string title = "Title";
        string content = TestConstants.PublicationContent;
        var createPublicationDto = new CreatePublicationDto()
        {
            Title = title,
            Content = content
        };
        var userIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.CreatePublicationAsync(userIdGuid, createPublicationDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);

        // Публикация и вправду не создалась | нахожу по автору и по DTO
        var publicationFromDbAfterCreate = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.AuthorId == userIdGuid && x.Title == title && x.Content == content);
        Assert.Null(publicationFromDbAfterCreate);
    }

    [Fact] // Перед записью в базу должно выбросится исключение, о том, что Publication невалидный
    public async Task CreatePublicationAsync_ThrowsInvalidOperationException_NotValidBeforeCreate()
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

        // Нет ошибок
        var validationResultCreatePublicationDto = new ValidationResult();

        // Какие-то ошибки
        var validationResultPublication = new ValidationResult()
        {
            Errors =
            [
                new ValidationFailure("PropertyName", "ErrorMessage")
            ]
        };

        _mockCreatePublicationDtoValidator.Setup(x => x.ValidateAsync(createPublicationDto, default)).ReturnsAsync(validationResultCreatePublicationDto);
        _mockPublicationValidator.Setup(x => x.ValidateAsync(It.IsAny<Publication>(), default)).ReturnsAsync(validationResultPublication); // Возвращаем ошибки валидации на любого Publication'а, который впихивается в ValidateAsync

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.CreatePublicationAsync(userIdGuid, createPublicationDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(Publication), validationResultPublication.Errors), ex.Message);
    }

    [Fact]
    public async Task CreatePublicationAsync_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        CreatePublicationDto createPublicationDto = null;
        var userIdGuid = Guid.NewGuid();

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.CreatePublicationAsync(userIdGuid, createPublicationDto);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(createPublicationDto), ex.ParamName);
    }


    [Theory]
    [InlineData("9fa85f64-5717-4562-b3fc-2c963f66afa6", TestConstants.EmptyGuidString)]
    [InlineData(TestConstants.EmptyGuidString, "9fa85f64-5717-4562-b3fc-2c963f66afa6")]
    [InlineData(TestConstants.EmptyGuidString, TestConstants.EmptyGuidString)]
    public async Task DeletePublicationAsync_NotValidGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier(string userId, string publicationId)
    {
        // Arrange
        var userIdGuid = Guid.Parse(userId);
        var publicationIdGuid = Guid.Parse(publicationId);

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.DeletePublicationAsync(userIdGuid, publicationIdGuid);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }


    [Fact]
    public async Task DeletePublicationAsyncByPublicationId_NotValidGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier()
    {
        // Arrange
        var publicationIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.DeletePublicationAsync(publicationIdGuid);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }


    [Fact]
    public async Task DeletePublicationsAsync_NotValidGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier()
    {
        // Arrange
        var userIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _publicationManager.DeletePublicationAsync(userIdGuid);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }


    [Theory]
    [InlineData("9fa85f64-5717-4562-b3fc-2c963f66afa6", TestConstants.EmptyGuidString)]
    [InlineData(TestConstants.EmptyGuidString, "9fa85f64-5717-4562-b3fc-2c963f66afa6")]
    [InlineData(TestConstants.EmptyGuidString, TestConstants.EmptyGuidString)]
    public async Task IsAuthorThisPublication_NotValidData_ReturnsFalse(string userId, string publicationId)
    {
        // Arrange
        var userIdGuid = Guid.Parse(userId);
        var publicationIdGuid = Guid.Parse(publicationId);

        // Act
        var result = await _publicationManager.IsAuthorThisPublicationAsync(userIdGuid, publicationIdGuid);

        // Assert
        Assert.False(result);
    }
}