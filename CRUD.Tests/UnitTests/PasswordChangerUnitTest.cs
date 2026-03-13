using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CRUD.Tests.UnitTests;

public class PasswordChangerUnitTest
{
    private readonly PasswordChanger _passwordChanger;
    private readonly ApplicationDbContext _db;
    private readonly Mock<IValidator<ChangePasswordDto>> _mockChangePasswordDtoValidator;
    private readonly Mock<IValidator<SetPasswordDto>> _mockSetPasswordDtoValidator;
    private readonly Mock<IValidator<User>> _mockUserValidator;
    private readonly Mock<IChangePasswordRequestManager> _mockChangePasswordRequestManager;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;

    public PasswordChangerUnitTest()
    {
        var db = DbContextGenerator.GenerateDbContextTestInMemory();
        _db = db;

        _mockChangePasswordDtoValidator = new();
        _mockSetPasswordDtoValidator = new();
        _mockUserValidator = new();
        _mockChangePasswordRequestManager = new();
        _mockPasswordHasher = new();

        _passwordChanger = new PasswordChanger(db, _mockChangePasswordDtoValidator.Object, _mockSetPasswordDtoValidator.Object, _mockUserValidator.Object, _mockChangePasswordRequestManager.Object, _mockPasswordHasher.Object);
    }

    [Fact]
    public async Task ChangePasswordAsync_NotValidGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier()
    {
        // Arrange
        string password = "";
        string newPassword = "";

        var changePasswordDto = new ChangePasswordDto()
        {
            Password = password,
            NewPassword = newPassword
        };
        var userIdGuid = Guid.Parse(TestConstants.EmptyGuidString);

        // Act
        Func<Task> a = async () =>
        {
            await _passwordChanger.ChangePasswordAsync(userIdGuid, changePasswordDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        ChangePasswordDto changePasswordDto = null;
        var userIdGuid = Guid.NewGuid();

        // Act
        Func<Task> a = async () =>
        {
            await _passwordChanger.ChangePasswordAsync(userIdGuid, changePasswordDto);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(changePasswordDto), ex.ParamName);
    }


    [Fact]
    public async Task ChangePasswordByTokenAsync_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        string token = null;

        // Act
        Func<Task> a = async () =>
        {
            await _passwordChanger.ChangePasswordAsync(token);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(token), ex.ParamName);
    }


    [Fact]
    public async Task SetPasswordAsync_NotValidGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier()
    {
        // Arrange
        string newPassword = "";

        var setPasswordDto = new SetPasswordDto()
        {
            NewPassword = newPassword
        };
        var userIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _passwordChanger.SetPasswordAsync(userIdGuid, setPasswordDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }

    [Fact]
    public async Task SetPasswordAsync_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        SetPasswordDto setPasswordDto = null;
        var userIdGuid = Guid.NewGuid();

        // Act
        Func<Task> a = async () =>
        {
            await _passwordChanger.SetPasswordAsync(userIdGuid, setPasswordDto);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(setPasswordDto), ex.ParamName);
    }
}