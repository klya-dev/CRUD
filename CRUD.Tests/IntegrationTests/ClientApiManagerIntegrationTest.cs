using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace CRUD.Tests.IntegrationTests;

[Collection(nameof(IntegrationsTestCollection))]
public sealed class ClientApiManagerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IClientApiManager _clientApiManager;
    private readonly ApplicationDbContext _db;

    public ClientApiManagerIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _clientApiManager = scopedServices.GetRequiredService<IClientApiManager>();
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
    }

    [Theory] // Корректные данные
    [InlineData("Title", TestConstants.PublicationContent, TestConstants.UserApiKey)]
    [InlineData("Ваще пофиг", TestConstants.PublicationContent, TestConstants.UserDisposableApiKey)]
    public async Task CreatePublicationAsync_ReturnsServiceResult(string title, string content, string apiKey)
    {
        // Arrange
        // Добавляем пользователя в базу
        await DI.CreateUserAsync(_db, isPremium: true, isEmailConfirm: true, isPhoneNumberConfirm: true, apiKey: apiKey, ct: TestContext.Current.CancellationToken);

        // Модель создания публикации по ключу
        var clientApiCreatePublicationDto = new ClientApiCreatePublicationDto()
        {
            Title = title,
            Content = content,
            ApiKey = apiKey
        };

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);

        // Act
        var result = await _clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Публикация и вправду создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);
        Assert.Null(publicationFromDbBeforeCreatePublication);
        Assert.NotNull(publicationFromDbAfterCreatePublication);
        Assert.Equivalent(clientApiCreatePublicationDto.Title, publicationFromDbAfterCreatePublication.Title);
        Assert.Equivalent(clientApiCreatePublicationDto.Content, publicationFromDbAfterCreatePublication.Content);
    }

    [Theory] // Невалидные данные
    [InlineData("Title", TestConstants.PublicationContent, "ApiKey")] // Неправильный API-ключ
    [InlineData(null, null, null)]
    public async Task CreatePublicationAsync_WhenNotValidData_ThrowsInvalidOperationException(string title, string content, string apiKey)
    {
        // Arrange
        var clientApiCreatePublicationDto = new ClientApiCreatePublicationDto()
        {
            Title = title,
            Content = content,
            ApiKey = apiKey
        };
        var validatorsLocalizer = new ValidatorLocalizer();
        var validationResult = await new ClientApiCreatePublicationDtoValidator(validatorsLocalizer).ValidateAsync(clientApiCreatePublicationDto, TestContext.Current.CancellationToken);

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);

        // Act
        Func<Task> a = async () =>
        {
            await _clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(ClientApiCreatePublicationDto), validationResult.Errors), ex.Message);

        // Публикация и вправду не создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);
        Assert.Null(publicationFromDbBeforeCreatePublication);
        Assert.Null(publicationFromDbAfterCreatePublication);
    }

    [Fact]
    public async Task CreatePublicationAsync_ReturnsErrorMessage_InvalidApiKey()
    {
        // Arrange
        string title = "Заголовок";
        string content = TestConstants.PublicationContent;
        string apiKey = TestConstants.UserInvalidApiKey;

        var clientApiCreatePublicationDto = new ClientApiCreatePublicationDto()
        {
            Title = title,
            Content = content,
            ApiKey = apiKey
        };

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);

        // Act
        var result = await _clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.InvalidApiKey, result.ErrorMessage);

        // Публикация и вправду не создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);
        Assert.Null(publicationFromDbBeforeCreatePublication);
        Assert.Null(publicationFromDbAfterCreatePublication);
    }

    [Fact]
    public async Task CreatePublicationAsync_ReturnsErrorMessage_UserDoesNotHavePremium()
    {
        // Arrange
        string title = "Заголовок";
        string content = TestConstants.PublicationContent;
        string apiKey = TestConstants.UserDisposableApiKey;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: false, isEmailConfirm: true, isPhoneNumberConfirm: true, apiKey: apiKey, ct: TestContext.Current.CancellationToken);

        var clientApiCreatePublicationDto = new ClientApiCreatePublicationDto()
        {
            Title = title,
            Content = content,
            ApiKey = apiKey
        };

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);

        // Act
        var result = await _clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserDoesNotHavePremium, result.ErrorMessage);

        // Публикация и вправду не создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);
        Assert.Null(publicationFromDbBeforeCreatePublication);
        Assert.Null(publicationFromDbAfterCreatePublication);
    }

    [Fact]
    public async Task CreatePublicationAsync_ReturnsErrorMessage_UserHasNotConfirmedEmail()
    {
        // Arrange
        string title = "Заголовок";
        string content = TestConstants.PublicationContent;
        string apiKey = TestConstants.UserDisposableApiKey;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: true, isEmailConfirm: false, isPhoneNumberConfirm: true, apiKey: apiKey, ct: TestContext.Current.CancellationToken);

        var clientApiCreatePublicationDto = new ClientApiCreatePublicationDto()
        {
            Title = title,
            Content = content,
            ApiKey = apiKey
        };

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);

        // Act
        var result = await _clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserHasNotConfirmedEmail, result.ErrorMessage);

        // Публикация и вправду не создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);
        Assert.Null(publicationFromDbBeforeCreatePublication);
        Assert.Null(publicationFromDbAfterCreatePublication);
    }

    [Fact]
    public async Task CreatePublicationAsync_ReturnsErrorMessage_UserHasNotConfirmedPhoneNumber()
    {
        // Arrange
        string title = "Заголовок";
        string content = TestConstants.PublicationContent;
        string apiKey = TestConstants.UserDisposableApiKey;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: true, isEmailConfirm: true, isPhoneNumberConfirm: false, apiKey: apiKey, ct: TestContext.Current.CancellationToken);

        var clientApiCreatePublicationDto = new ClientApiCreatePublicationDto()
        {
            Title = title,
            Content = content,
            ApiKey = apiKey
        };

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);

        // Act
        var result = await _clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserHasNotConfirmedPhoneNumber, result.ErrorMessage);

        // Публикация и вправду не создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content, TestContext.Current.CancellationToken);
        Assert.Null(publicationFromDbBeforeCreatePublication);
        Assert.Null(publicationFromDbAfterCreatePublication);
    }
}