#nullable disable

using CRUD.Models.Validators;
using CRUD.Models.Validators.ValidatorsLocalizer;

namespace CRUD.Tests.IntegrationTests.Validators;

public class LoginDataValidatorTest
{
    // #nullable disable

    private readonly ValidatorsLocalizer _validatorsLocalizer;
    private readonly LoginDataDtoValidator _validator;

    public LoginDataValidatorTest()
    {
        _validatorsLocalizer = new ValidatorsLocalizer();
        _validator = new LoginDataDtoValidator(_validatorsLocalizer);
    }

    [Theory]
    [InlineData("username", "abc123")] // Корректные данные
    [InlineData("vladik", "qwerty@102$")]
    [InlineData("superjoj", "кириллиц*а!")]
    public async Task ValidateAsync_CorrectData_ReturnsIsValid(string username, string password)
    {
        // Arrange
        var loginData = new LoginDataDto()
        {
            Username = username,
            Password = password
        };

        // Act
        var result = await _validator.ValidateAsync(loginData);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("", "password")] // Пустой Username
    [InlineData(null, "password")] // Пустой Username
    [InlineData("\t", "password")] // Пустой Username
    //[InlineData("u", "password")] // Username меньше 4 символов
    //[InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz", "password")] // Имя больше 32 символов
    //[InlineData("username123", "password")] // Username содержит цифры
    //[InlineData("$username@", "password")] // Username содержит специальные символы
    //[InlineData("юзернейм", "password")] // Username из кириллицы
    //[InlineData("юзернейм_2", "password")] // Username из кириллицы
    //[InlineData("username twice", "password")] // Username с пробелом
    //[InlineData("юзерname", "password")] // Username из кириллицы и латиницы

    [InlineData("username", "")] // Пустой Пароль
    [InlineData("username", null)] // Пустой Пароль
    [InlineData("username", "\t")] // Пустой Пароль

    [InlineData(null, null)] // Пустые данные

    [InlineData("", null)] // Пустой Username и Пароль
    //[InlineData("$username@", "qwerty@102$")] // Невалидный Username
    //[InlineData("юзернейм_2", "\t")] // Невалидный Username и Пароль
    public async Task ValidateAsync_NotValidData_ReturnsIsNotValid(string username, string password)
    {
        // Arrange
        var loginData = new LoginDataDto()
        {
            Username = username,
            Password = password
        };

        // Act
        var result = await _validator.ValidateAsync(loginData);

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
        LoginDataDto loginData = null;

        // Act
        Func<Task> a = async () =>
        {
            await _validator.ValidateAsync(loginData);
        };

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(a);
    }
}