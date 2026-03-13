#nullable disable
using CRUD;
using CRUD.Models.Validators;

namespace CRUD.Tests.IntegrationTests.Validators.User;

public class DeleteUserDtoValidatorTest
{
    // #nullable disable

    private readonly DeleteUserDtoValidator _validator;

    public DeleteUserDtoValidatorTest()
    {
        var validatorsLocalizer = new Models.Validators.ValidatorsLocalizer.ValidatorsLocalizer();
        _validator = new DeleteUserDtoValidator(validatorsLocalizer);
    }

    [Theory]
    [InlineData("qwerty100")] // Корректные данные
    [InlineData("password")]
    [InlineData("_LOL_")]
    [InlineData("W!@#$E")]
    [InlineData("даже кириллица")]
    public async Task ValidateAsync_CorrectData_ReturnsIsValid(string password)
    {
        // Arrange
        var deleteUserDto = new DeleteUserDto()
        {
            Password = password
        };

        // Act
        var result = await _validator.ValidateAsync(deleteUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("")] // Пустой Пароль
    [InlineData(null)] // Пустой Пароль
    [InlineData("\t")] // Пустой Пароль
    public async Task ValidateAsync_NotValidData_ReturnsIsNotValid(string password)
    {
        // Arrange
        var deleteUserDto = new DeleteUserDto()
        {
            Password = password
        };

        // Act
        var result = await _validator.ValidateAsync(deleteUserDto);

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
        DeleteUserDto deleteUserDto = null;

        // Act
        Func<Task> a = async () =>
        {
            await _validator.ValidateAsync(deleteUserDto);
        };

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(a);
    }
}