using FluentValidation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace CRUD.Tests.UnitTests;

public sealed class PasswordChangerUnitTest
{
    private readonly PasswordChanger _passwordChanger;
    private readonly ApplicationDbContext _db;
    private readonly Mock<IValidator<ChangePasswordDto>> _mockChangePasswordDtoValidator;
    private readonly Mock<IValidator<SetPasswordDto>> _mockSetPasswordDtoValidator;
    private readonly Mock<IChangePasswordRequestManager> _mockChangePasswordRequestManager;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IDataProtectionProvider> _mockProtectionProvider;

    public PasswordChangerUnitTest()
    {
        var db = DbContextGenerator.GenerateDbContextTestInMemory();
        _db = db;

        _mockChangePasswordDtoValidator = new();
        _mockSetPasswordDtoValidator = new();
        _mockChangePasswordRequestManager = new();
        _mockPasswordHasher = new();
        _mockProtectionProvider = new();

        var mockProtector = new Mock<IDataProtector>();

        // Настраиваем методы защиты и расшифровки данных
        mockProtector
            .Setup(s => s.Protect(It.IsAny<byte[]>()))
            .Returns((byte[] input) => input); // В тесте возвращаем массив как есть

        mockProtector
            .Setup(s => s.Unprotect(It.IsAny<byte[]>()))
            .Returns((byte[] input) => input);

        // Связываем провайдер с протектором
        _mockProtectionProvider
            .Setup(x => x.CreateProtector(It.IsAny<string>()))
            .Returns(mockProtector.Object);

        _passwordChanger = new PasswordChanger(db, _mockChangePasswordDtoValidator.Object, _mockSetPasswordDtoValidator.Object, _mockChangePasswordRequestManager.Object, _mockPasswordHasher.Object, _mockProtectionProvider.Object);
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