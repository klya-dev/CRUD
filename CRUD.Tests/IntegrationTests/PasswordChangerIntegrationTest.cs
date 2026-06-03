using FluentValidation;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace CRUD.Tests.IntegrationTests;

public sealed class PasswordChangerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IPasswordChanger _passwordChanger;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ApplicationDbContext _db;

    public PasswordChangerIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _passwordChanger = scopedServices.GetRequiredService<IPasswordChanger>();
        _passwordHasher = scopedServices.GetRequiredService<IPasswordHasher>();
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
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

        // Добавляем токен в базу, чтобы возникла ошибка, что письмо уже отправлено
        var changePasswordRequest = await DI.CreateChangePasswordRequestAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

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
    }


    [Fact]
    public async Task ChangePasswordAsyncByToken_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var changePasswordRequest = await DI.CreateChangePasswordRequestAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _passwordChanger.ChangePasswordAsync(changePasswordRequest.Token, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Пароль и вправду обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
        Assert.Equal(changePasswordRequest.HashedNewPassword, userFromDbAfterUpdate.HashedPassword);
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

        // Добавляем токен в базу
        var changePasswordRequest = await DI.CreateChangePasswordRequestAsync(_db, userIdGuid, expires: DateTime.UtcNow.AddDays(-1), ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _passwordChanger.ChangePasswordAsync(changePasswordRequest.Token, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.InvalidToken, result.ErrorMessage);
    }

    [Fact] // Такого пользователя не существует (т.к мы удалим пользователя). У токена не может быть несуществующего пользователя, поэтому токен автоматически удаляется
    public async Task ChangePasswordAsyncByToken_WhenUserDeleted_ReturnsErrorMessage_InvalidToken()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var changePasswordRequest = await DI.CreateChangePasswordRequestAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);

        // Удаляем пользователя
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Тут токен удаляется автоматически сам

        // Act
        var result = await _passwordChanger.ChangePasswordAsync(changePasswordRequest.Token, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.InvalidToken, result.ErrorMessage);
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