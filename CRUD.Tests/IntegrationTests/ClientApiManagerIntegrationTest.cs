#nullable disable
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace CRUD.Tests.IntegrationTests;

public class ClientApiManagerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    // #nullable disable

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

    private IClientApiManager GenerateNewClientApiManager()
    {
        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        return scopedServices.GetRequiredService<IClientApiManager>();
    }

    [Theory] // Корректные данные
    [InlineData("Title", TestConstants.PublicationContent, TestConstants.UserApiKey)]
    [InlineData("Ваще пофиг", TestConstants.PublicationContent, TestConstants.UserDisposableApiKey)]
    public async Task CreatePublicationAsync_ReturnsServiceResult(string title, string content, string apiKey)
    {
        // Arrange
        // Добавляем пользователя в базу
        await DI.CreateUserAsync(_db, isPremium: true, isEmailConfirm: true, isPhoneNumberConfirm: true, apiKey: apiKey);

        // Модель создания публикации по ключу
        var clientApiCreatePublicationDto = new ClientApiCreatePublicationDto()
        {
            Title = title,
            Content = content,
            ApiKey = apiKey
        };

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);

        // Act
        var result = await _clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Публикация и вправду создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);
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
        var validatorsLocalizer = new Models.Validators.ValidatorsLocalizer.ValidatorsLocalizer();
        var validationResult = await new ClientApiCreatePublicationDtoValidator(validatorsLocalizer).ValidateAsync(clientApiCreatePublicationDto);

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);

        // Act
        Func<Task> a = async () =>
        {
            await _clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(ClientApiCreatePublicationDto), validationResult.Errors), ex.Message);

        // Публикация и вправду не создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);
        Assert.Null(publicationFromDbBeforeCreatePublication);
        Assert.Null(publicationFromDbAfterCreatePublication);
    }

    [Fact] // Перед записью в базу выбросится исключение, о том, что User невалидный
    public async Task CreatePublicationAsync_ThrowsInvalidOperationException_NotValidBeforeUpdate()
    {
        // Arrange
        string title = "Заголовок";
        string content = TestConstants.PublicationContent;
        string apiKey = TestConstants.UserApiKey;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: true, isEmailConfirm: true, isPhoneNumberConfirm: true, apiKey: apiKey, role: "НЕВАЛИДНАЯ РОЛЬ");

        var clientApiCreatePublicationDto = new ClientApiCreatePublicationDto()
        {
            Title = title,
            Content = content,
            ApiKey = apiKey
        };

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);

        // Результат валидации (о том, что роль невалидна)
        var validationResult = await new UserValidator().ValidateAsync(user);

        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);

        // Act
        Func<Task> a = async () =>
        {
            await _clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(User), validationResult.Errors), ex.Message);

        // Публикация и вправду создалась
        var userFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);
        Assert.Null(publicationFromDbBeforeCreatePublication);
        Assert.NotNull(userFromDbAfterCreatePublication);
        Assert.Equivalent(clientApiCreatePublicationDto.Title, userFromDbAfterCreatePublication.Title);
        Assert.Equivalent(clientApiCreatePublicationDto.Content, userFromDbAfterCreatePublication.Content);

        // Пользователь и вправду не обновился (после манипуляций с ролью)
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);
        Assert.Equivalent(userFromDbBeforeUpdate, userFromDbAfterUpdate);

        // А это значит, что публикация создалась, а вот одноразовый API-ключ (если он был предоставлен) нет
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
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);

        // Act
        var result = await _clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.InvalidApiKey, result.ErrorMessage);

        // Публикация и вправду не создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);
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
        var user = await DI.CreateUserAsync(_db, isPremium: false, isEmailConfirm: true, isPhoneNumberConfirm: true, apiKey: apiKey);

        var clientApiCreatePublicationDto = new ClientApiCreatePublicationDto()
        {
            Title = title,
            Content = content,
            ApiKey = apiKey
        };

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);

        // Act
        var result = await _clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserDoesNotHavePremium, result.ErrorMessage);

        // Публикация и вправду не создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);
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
        var user = await DI.CreateUserAsync(_db, isPremium: true, isEmailConfirm: false, isPhoneNumberConfirm: true, apiKey: apiKey);

        var clientApiCreatePublicationDto = new ClientApiCreatePublicationDto()
        {
            Title = title,
            Content = content,
            ApiKey = apiKey
        };

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);

        // Act
        var result = await _clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserHasNotConfirmedEmail, result.ErrorMessage);

        // Публикация и вправду не создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);
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
        var user = await DI.CreateUserAsync(_db, isPremium: true, isEmailConfirm: true, isPhoneNumberConfirm: false, apiKey: apiKey);

        var clientApiCreatePublicationDto = new ClientApiCreatePublicationDto()
        {
            Title = title,
            Content = content,
            ApiKey = apiKey
        };

        // Публикации не должно существовать, до создания
        var publicationFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);

        // Act
        var result = await _clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserHasNotConfirmedPhoneNumber, result.ErrorMessage);

        // Публикация и вправду не создалась
        var publicationFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);
        Assert.Null(publicationFromDbBeforeCreatePublication);
        Assert.Null(publicationFromDbAfterCreatePublication);
    }


    // Конфликты параллельности


    [Theory] // Корректные данные
    [InlineData("Title", TestConstants.PublicationContent, TestConstants.UserApiKey)]
    [InlineData("Ваще пофиг", TestConstants.PublicationContent, TestConstants.UserDisposableApiKey)]
    public async Task CreatePublicationAsync_ConcurrencyConflict_ReturnsErrorMessage_NothingOrConflictOrInvalidApiKey(string title, string content, string apiKey)
    {
        // Arrange
        // Добавляем пользователя в базу
        await DI.CreateUserAsync(_db, isPremium: true, isEmailConfirm: true, isPhoneNumberConfirm: true, apiKey: apiKey);

        var clientApiCreatePublicationDto = new ClientApiCreatePublicationDto()
        {
            Title = title,
            Content = content,
            ApiKey = apiKey
        };
        var clientApiManager = GenerateNewClientApiManager();
        var clientApiManager2 = GenerateNewClientApiManager();

        // Публикации не должно существовать, до создания
        var userFromDbBeforeCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);

        // Act
        var task = clientApiManager.CreatePublicationAsync(clientApiCreatePublicationDto);
        var task2 = clientApiManager2.CreatePublicationAsync(clientApiCreatePublicationDto);

        // Может выбросится исключение с конфликтом параллельности
        try
        {
            var results = await Task.WhenAll(task, task2);

            // Assert
            foreach (var result in results)
            {
                Assert.NotNull(result);

                // Либо ничего, либо неверный API-ключ (если был одноразовый и он успел изменится)
                var errorMessage = result.ErrorMessage;
                string[] allowedErrors =
                [
                    null,
                    ErrorMessages.InvalidApiKey
                ];

                Assert.Contains(errorMessage, allowedErrors);
            }

            // Публикация и вправду создалась
            var userFromDbAfterCreatePublication = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title && x.Content == content);
            Assert.Null(userFromDbBeforeCreatePublication);
            Assert.NotNull(userFromDbAfterCreatePublication);
            Assert.Equivalent(clientApiCreatePublicationDto.Title, userFromDbAfterCreatePublication.Title);
            Assert.Equivalent(clientApiCreatePublicationDto.Content, userFromDbAfterCreatePublication.Content);
        }
        catch (DbUpdateException ex)
        {
            // Если не конфликт параллельности, не обрабатываем
            if (!DbExceptionHelper.IsConcurrencyConflict(ex))
                throw;
        }
    }
}