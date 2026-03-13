#nullable disable

using CRUD;
using CRUD.Models.Validators;

namespace CRUD.Tests.IntegrationTests.Validators;

public class ChangePasswordDtoValidatorTest
{
    // #nullable disable

    private readonly ChangePasswordDtoValidator _validator;

    public ChangePasswordDtoValidatorTest()
    {
        var validatorsLocalizer = new Models.Validators.ValidatorsLocalizer.ValidatorsLocalizer();
        _validator = new ChangePasswordDtoValidator(validatorsLocalizer);
    }

    [Theory]
    [InlineData("qwerty", "qwerty100")] // Корректные данные
    [InlineData("1", "12345")]
    [InlineData("p17D", "Yrew43#$_12_*&")]
    [InlineData("password", "Pass_word")]
    public async Task ValidateAsync_CorrectData_ReturnsIsValid(string password, string newPassword)
    {
        // Arrange
        var changePasswordDto = new ChangePasswordDto()
        {
            Password = password,
            NewPassword = newPassword
        };

        // Act
        var result = await _validator.ValidateAsync(changePasswordDto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("", "newPassword")] // Пустой Пароль
    [InlineData(null, "newPassword")] // Пустой Пароль
    [InlineData("\t", "newPassword")] // Пустой Пароль

    [InlineData("password", "")] // Пустой Новый Пароль
    [InlineData("password", null)] // Пустой Новый Пароль
    [InlineData("password", "\t")] // Пустой Новый Пароль
    [InlineData("password", "Qw")] // Новый Пароль меньше 4 символов
    [InlineData("password", "qqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqq")] // Новый Пароль больше 32 символов
    [InlineData("password", "Pass word")] // Новый Пароль с пробелом
    [InlineData("password", "парольTop")] // Новый Пароль из кириллицы и латиницы
    [InlineData("password", "Pass_(word)")] // Новый Пароль со скобками

    [InlineData(null, null)] // Пустые данные

    [InlineData(null, "qwerty100")] // Невалидный Пароль
    [InlineData("    \t \n  ", "12345")] // Невалидный Пароль
    [InlineData("qwerty", "Pass_(word)")] // Невалидный Новый Пароль
    [InlineData("p17D", "парольTop")] // Невалидный Новый Пароль
    [InlineData("  \t", "Pass word")] // Невалидный Пароль и Новый Пароль
    public async Task ValidateAsync_NotValidData_ReturnsIsNotValid(string password, string newPassword)
    {
        // Arrange
        var changePasswordDto = new ChangePasswordDto()
        {
            Password = password,
            NewPassword = newPassword
        };

        // Act
        var result = await _validator.ValidateAsync(changePasswordDto);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_NullObject_ThrowsInvalidOperationException()
    {
        // Arrange
        ChangePasswordDto changePasswordDto = null;

        // Act
        Func<Task> a = async () =>
        {
            await _validator.ValidateAsync(changePasswordDto);
        };

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(a);
    }
}