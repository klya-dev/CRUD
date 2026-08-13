using FluentValidation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using System.Text.Json;

namespace CRUD.Tests.IntegrationTests;

[Collection(nameof(IntegrationsTestCollection))]
public sealed class PasswordChangerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IPasswordChanger _passwordChanger;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ApplicationDbContext _db;
    private readonly ITimeLimitedDataProtector _protector;
    private readonly HybridCache _cache;

    public PasswordChangerIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _passwordChanger = scopedServices.GetRequiredService<IPasswordChanger>();
        _passwordHasher = scopedServices.GetRequiredService<IPasswordHasher>();
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _protector = scopedServices.GetRequiredService<IDataProtectionProvider>().CreateProtector(ChangePasswordRequestManager.Purpose).ToTimeLimitedDataProtector();
        _cache = scopedServices.GetRequiredService<HybridCache>();
    }

    [Theory]
    [InlineData("123", "!123@L")]
    [InlineData("kekpass", "newsuperpassword")]
    public async Task ChangePasswordAsync_ReturnsServiceResult(string password, string newPassword)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, hashedPassword: password, ct: TestContext.Current.CancellationToken);

        var changePasswordDto = new ChangePasswordDto()
        {
            Password = password,
            NewPassword = newPassword
        };

        var userIdGuid = user.Id;

        // Act
        var result = await _passwordChanger.ChangePasswordAsync(userIdGuid, changePasswordDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Пароль и вправду не обновился (т.к нужно подтверждение)
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
        Assert.False(_passwordHasher.Verify(changePasswordDto.NewPassword, userFromDbAfterUpdate.HashedPassword));
    }

    [Theory]
    [InlineData("123", "kek")] // Невалидный новый пароль
    public async Task ChangePasswordAsync_NotValidData_ThrowsInvalidOperationException(string password, string newPassword)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, hashedPassword: password, ct: TestContext.Current.CancellationToken);

        var changePasswordDto = new ChangePasswordDto()
        {
            Password = password,
            NewPassword = newPassword
        };
        var userIdGuid = user.Id;
        var validatorsLocalizer = new ValidatorLocalizer();
        var validationResult = await new ChangePasswordDtoValidator(validatorsLocalizer).ValidateAsync(changePasswordDto, TestContext.Current.CancellationToken);
        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);

        // Act
        Func<Task> a = async () =>
        {
            await _passwordChanger.ChangePasswordAsync(userIdGuid, changePasswordDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(ChangePasswordDto), validationResult.Errors), ex.Message);

        // Пароль и вправду не обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
        Assert.Equivalent(userFromDbBeforeUpdate, userFromDbAfterUpdate);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        string password = "123";
        string newPassword = "!123@L";

        var changePasswordDto = new ChangePasswordDto()
        {
            Password = password,
            NewPassword = newPassword
        };
        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _passwordChanger.ChangePasswordAsync(userIdGuid, changePasswordDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsErrorMessage_InvalidPassword()
    {
        // Arrange
        string password = "123";
        string newPassword = "!123@L";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, hashedPassword: password + "SOMETHING_WRONG", ct: TestContext.Current.CancellationToken);

        var changePasswordDto = new ChangePasswordDto()
        {
            Password = password,
            NewPassword = newPassword
        };
        var userIdGuid = user.Id;
        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);

        // Act
        var result = await _passwordChanger.ChangePasswordAsync(userIdGuid, changePasswordDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.InvalidPassword, result.ErrorMessage);

        // Пароль и вправду не обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
        Assert.Equivalent(userFromDbBeforeUpdate, userFromDbAfterUpdate);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsErrorMessage_LetterAlreadySent()
    {
        // Arrange
        string password = "123";
        string newPassword = "!123@L";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, hashedPassword: password, ct: TestContext.Current.CancellationToken);

        // Добавляем время отправки в кэш, чтобы возникла ошибка, что письмо уже отправлено
        string cacheKey = $"{CacheKeys.RateLimitSendEmailPasswordChange}-{user.Id}";
        var cacheOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(1),
            LocalCacheExpiration = TimeSpan.FromMinutes(1)
        };
        await _cache.SetAsync(cacheKey, DateTime.UtcNow, cacheOptions, cancellationToken: TestContext.Current.CancellationToken);

        // Если L2 кэш не включен, то IMemoryCache лежит, я так полагаю, в разных местах, из-за этого, якобы кэш не добавляется, и метод внутри приложения не видит значение

        var changePasswordDto = new ChangePasswordDto()
        {
            Password = password,
            NewPassword = newPassword
        };
        var userIdGuid = user.Id;
        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);

        // Act
        var result = await _passwordChanger.ChangePasswordAsync(userIdGuid, changePasswordDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.LetterAlreadySent, result.ErrorMessage);

        // Пароль и вправду не обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
        Assert.Equivalent(userFromDbBeforeUpdate, userFromDbAfterUpdate);

        // Удаляем из кэша
        await _cache.RemoveAsync(cacheKey, cancellationToken: TestContext.Current.CancellationToken);
    }


    [Fact]
    public async Task ChangePasswordAsyncByToken_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var changePasswordPayload = DI.CreateChangePasswordPayload(userIdGuid);
        var token = _protector.Protect(JsonSerializer.Serialize(changePasswordPayload));

        // Act
        var result = await _passwordChanger.ChangePasswordAsync(token, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Пароль и вправду обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
        Assert.Equal(changePasswordPayload.HashedNewPassword, userFromDbAfterUpdate.HashedPassword);
    }

    [Fact]
    public async Task ChangePasswordAsyncByToken_ReturnsErrorMessage_InvalidToken()
    {
        // Arrange
        string token = "token";

        // Act
        var result = await _passwordChanger.ChangePasswordAsync(token, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.InvalidToken, result.ErrorMessage);
    }

    [Fact]
    public async Task ChangePasswordAsyncByToken_InvalidCreatedAt_ReturnsErrorMessage_InvalidToken()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Истёкший токен
        var payload = DI.CreateChangePasswordPayload(userIdGuid);
        string payloadJson = JsonSerializer.Serialize(payload);
        string token = _protector.Protect(payloadJson, TimeSpan.FromMinutes(-60));

        // Act
        var result = await _passwordChanger.ChangePasswordAsync(token, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.InvalidToken, result.ErrorMessage);
    }

    [Fact] // Такого пользователя не существует
    public async Task ChangePasswordAsyncByToken_WhenUserDeleted_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Валидный токен
        var payload = DI.CreateChangePasswordPayload(userIdGuid);
        string payloadJson = JsonSerializer.Serialize(payload);
        string token = _protector.Protect(payloadJson, TimeSpan.FromMinutes(1));

        // Удаляем пользователя
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _passwordChanger.ChangePasswordAsync(token, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }


    [Theory]
    [InlineData("!123@L")]
    [InlineData("newsuperpassword")]
    public async Task SetPasswordAsync_ReturnsServiceResult(string newPassword)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var setPasswordDto = new SetPasswordDto()
        {
            NewPassword = newPassword
        };

        var userIdGuid = user.Id;

        // Act
        var result = await _passwordChanger.SetPasswordAsync(userIdGuid, setPasswordDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Пароль и вправду обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
        Assert.True(_passwordHasher.Verify(setPasswordDto.NewPassword, userFromDbAfterUpdate.HashedPassword));
    }

    [Theory]
    [InlineData("kek")] // Невалидный новый пароль
    public async Task SetPasswordAsync_NotValidData_ThrowsInvalidOperationException(string newPassword)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var setPasswordDto = new SetPasswordDto()
        {
            NewPassword = newPassword
        };

        var userIdGuid = user.Id;

        var validatorsLocalizer = new ValidatorLocalizer();
        var validationResult = await new SetPasswordDtoValidator(validatorsLocalizer).ValidateAsync(setPasswordDto, TestContext.Current.CancellationToken);
        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);

        // Act
        Func<Task> a = async () =>
        {
            await _passwordChanger.SetPasswordAsync(userIdGuid, setPasswordDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(SetPasswordDto), validationResult.Errors), ex.Message);

        // Пароль и вправду не обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
        Assert.Equivalent(userFromDbBeforeUpdate, userFromDbAfterUpdate);
    }

    [Fact]
    public async Task SetPasswordAsync_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        string newPassword = "!123@L";

        var setPasswordDto = new SetPasswordDto()
        {
            NewPassword = newPassword
        };

        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _passwordChanger.SetPasswordAsync(userIdGuid, setPasswordDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }
}