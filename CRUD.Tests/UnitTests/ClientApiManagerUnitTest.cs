using CRUD.Models.Domains;
using FluentValidation;

namespace CRUD.Tests.UnitTests;

public class ClientApiManagerUnitTest
{
    private readonly ClientApiManager _clientApiManager;
    private readonly ApplicationDbContext _db;
    private readonly Mock<IPublicationManager> _mockPublicationManager;
    private readonly Mock<IUserApiKeyManager> _mockUserApiKeyManager;
    private readonly Mock<IValidator<ClientApiCreatePublicationDto>> _mockClientApiCreatePublicationDtoValidator;
    private readonly Mock<IValidator<User>> _mockUserValidator;

    public ClientApiManagerUnitTest()
    {
        var db = DbContextGenerator.GenerateDbContextTestInMemory();
        _db = db;

        _mockPublicationManager = new();
        _mockUserApiKeyManager = new();
        _mockClientApiCreatePublicationDtoValidator = new();
        _mockUserValidator = new();

        _clientApiManager = new ClientApiManager(db, _mockPublicationManager.Object, _mockUserApiKeyManager.Object, _mockClientApiCreatePublicationDtoValidator.Object, _mockUserValidator.Object);
    }

    [Fact]
    public async Task CreatePublicationAsync_ReturnsServiceResult()
    {
        // Arrange
        string title = "Title";
        string content = TestConstants.PublicationContent;
        string apiKey = TestConstants.UserApiKey;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: true, isEmailConfirm: true, isPhoneNumberConfirm: true, apiKey: apiKey);

        // Модель создания публикации по ключу
        var clientApiCreatePublicationDto = new ClientApiCreatePublicationDto()
        {
            Title = title,
            Content = content,
            ApiKey = apiKey
        };

        // Валидация проходит
        _mockClientApiCreatePublicationDtoValidator.Setup(x => x.ValidateAsync(It.IsAny<ClientApiCreatePublicationDto>(), default)).ReturnsAsync(new ValidationResult());

        // Успешное создание публикации
        var publicationDto = new PublicationDto() { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, EditedAt = null, Title = title, Content = content, AuthorId = user.Id, AuthorFirstname = user.Firstname }; // В целом, без разницы, это нужно чисто для ответа API
        _mockPublicationManager.Setup(x => x.CreatePublicationAsync(It.IsAny<Guid>(), It.IsAny<CreatePublicationDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<PublicationDto>.Success(publicationDto));

        // Валидация проходит
        _mockUserValidator.Setup(x => x.ValidateAsync(It.IsAny<User>(), default)).ReturnsAsync(new ValidationResult());

        // Act
        var result = await _clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task CreatePublicationAsync_WhenNotValidData_ThrowsInvalidOperationException()
    {
        // Arrange
        string title = "Title";
        string content = TestConstants.PublicationContent;
        string apiKey = "ApiKey";

        // Модель создания публикации по ключу
        var clientApiCreatePublicationDto = new ClientApiCreatePublicationDto()
        {
            Title = title,
            Content = content,
            ApiKey = apiKey
        };

        // Валидация не проходит
        var validationResult = new ValidationResult() { Errors = [new ValidationFailure()] };
        _mockClientApiCreatePublicationDtoValidator.Setup(x => x.ValidateAsync(It.IsAny<ClientApiCreatePublicationDto>(), default)).ReturnsAsync(validationResult);

        // Act
        Func<Task> a = async () =>
        {
            await _clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(ClientApiCreatePublicationDto), validationResult.Errors), ex.Message);
    }

    [Fact] // Перед записью в базу выбросится исключение, о том, что User невалидный
    public async Task CreatePublicationAsync_ThrowsInvalidOperationException_NotValidBeforeUpdate()
    {
        // Arrange
        string title = "Title";
        string content = TestConstants.PublicationContent;
        string apiKey = "ApiKey";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: true, isEmailConfirm: true, isPhoneNumberConfirm: true, apiKey: apiKey);

        // Модель создания публикации по ключу
        var clientApiCreatePublicationDto = new ClientApiCreatePublicationDto()
        {
            Title = title,
            Content = content,
            ApiKey = apiKey
        };

        // Валидация проходит
        _mockClientApiCreatePublicationDtoValidator.Setup(x => x.ValidateAsync(It.IsAny<ClientApiCreatePublicationDto>(), default)).ReturnsAsync(new ValidationResult());

        // Успешное создание публикации
        var publicationDto = new PublicationDto() { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, EditedAt = null, Title = title, Content = content, AuthorId = user.Id, AuthorFirstname = user.Firstname }; // В целом, без разницы, это нужно чисто для ответа API
        _mockPublicationManager.Setup(x => x.CreatePublicationAsync(It.IsAny<Guid>(), It.IsAny<CreatePublicationDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<PublicationDto>.Success(publicationDto));

        // Валидация не проходит
        var validationResult = new ValidationResult() { Errors = [new ValidationFailure()] };
        _mockUserValidator.Setup(x => x.ValidateAsync(It.IsAny<User>(), default)).ReturnsAsync(validationResult);

        // Act
        Func<Task> a = async () =>
        {
            await _clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(User), validationResult.Errors), ex.Message);

        // А это значит, что публикация создалась, а вот одноразовый API-ключ (если он был предоставлен) нет
    }

    [Fact]
    public async Task CreatePublicationAsync_WhenInvalidApiKey_ReturnsErrorMessage_InvalidApiKey()
    {
        // Arrange
        string title = "Title";
        string content = TestConstants.PublicationContent;
        string apiKey = "ApiKey";

        // Модель создания публикации по ключу
        var clientApiCreatePublicationDto = new ClientApiCreatePublicationDto()
        {
            Title = title,
            Content = content,
            ApiKey = apiKey
        };

        // Валидация проходит
        _mockClientApiCreatePublicationDtoValidator.Setup(x => x.ValidateAsync(It.IsAny<ClientApiCreatePublicationDto>(), default)).ReturnsAsync(new ValidationResult());

        // Act
        var result = await _clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.InvalidApiKey, result.ErrorMessage);
    }

    [Fact]
    public async Task CreatePublicationAsync_WhenUserDoesNotHavePremium_ReturnsErrorMessage_UserDoesNotHavePremium()
    {
        // Arrange
        string title = "Title";
        string content = TestConstants.PublicationContent;
        string apiKey = "ApiKey";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: false, isEmailConfirm: true, isPhoneNumberConfirm: true, apiKey: apiKey);

        // Модель создания публикации по ключу
        var clientApiCreatePublicationDto = new ClientApiCreatePublicationDto()
        {
            Title = title,
            Content = content,
            ApiKey = apiKey
        };

        // Валидация проходит
        _mockClientApiCreatePublicationDtoValidator.Setup(x => x.ValidateAsync(It.IsAny<ClientApiCreatePublicationDto>(), default)).ReturnsAsync(new ValidationResult());

        // Act
        var result = await _clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserDoesNotHavePremium, result.ErrorMessage);
    }

    [Fact]
    public async Task CreatePublicationAsync_WhenUserHasNotConfirmedEmail_ReturnsErrorMessage_UserHasNotConfirmedEmail()
    {
        // Arrange
        string title = "Title";
        string content = TestConstants.PublicationContent;
        string apiKey = "ApiKey";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: true, isEmailConfirm: false, isPhoneNumberConfirm: true, apiKey: apiKey);

        // Модель создания публикации по ключу
        var clientApiCreatePublicationDto = new ClientApiCreatePublicationDto()
        {
            Title = title,
            Content = content,
            ApiKey = apiKey
        };

        // Валидация проходит
        _mockClientApiCreatePublicationDtoValidator.Setup(x => x.ValidateAsync(It.IsAny<ClientApiCreatePublicationDto>(), default)).ReturnsAsync(new ValidationResult());

        // Act
        var result = await _clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserHasNotConfirmedEmail, result.ErrorMessage);
    }

    [Fact]
    public async Task CreatePublicationAsync_WhenUserHasNotConfirmedPhoneNumber_ReturnsErrorMessage_UserHasNotConfirmedPhoneNumber()
    {
        // Arrange
        string title = "Title";
        string content = TestConstants.PublicationContent;
        string apiKey = "ApiKey";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: true, isEmailConfirm: true, isPhoneNumberConfirm: false, apiKey: apiKey);

        // Модель создания публикации по ключу
        var clientApiCreatePublicationDto = new ClientApiCreatePublicationDto()
        {
            Title = title,
            Content = content,
            ApiKey = apiKey
        };

        // Валидация проходит
        _mockClientApiCreatePublicationDtoValidator.Setup(x => x.ValidateAsync(It.IsAny<ClientApiCreatePublicationDto>(), default)).ReturnsAsync(new ValidationResult());

        // Act
        var result = await _clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserHasNotConfirmedPhoneNumber, result.ErrorMessage);
    }

    [Fact]
    public async Task CreatePublicationAsync_WhenNullObject_ThrowsArgumentNullException()
    {
        // Arrange
        ClientApiCreatePublicationDto clientApiCreatePublicationDto = null;

        // Act
        Func<Task> a = async () =>
        {
            await _clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(clientApiCreatePublicationDto), ex.ParamName);
    }
}