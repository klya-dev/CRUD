#nullable disable
using CRUD;
using CRUD.Models.Validators;
using CRUD.Tests.Helpers;

namespace CRUD.Tests.IntegrationTests.Validators.Publication;

public class CreatePublicationDtoValidatorTest
{
    // #nullable disable

    private readonly CreatePublicationDtoValidator _validator;

    public CreatePublicationDtoValidatorTest()
    {
        var validatorsLocalizer = new Models.Validators.ValidatorsLocalizer.ValidatorsLocalizer();
        _validator = new CreatePublicationDtoValidator(validatorsLocalizer);
    }

    [Theory]
    [InlineData("Заголовок", TestConstants.PublicationContent)] // Корректные данные
    public async Task ValidateAsync_CorrectData_ReturnsIsValid(string title, string content)
    {
        // Arrange
        var createPublicationDto = new CreatePublicationDto()
        {
            Title = title,
            Content = content
        };

        // Act
        var result = await _validator.ValidateAsync(createPublicationDto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("", TestConstants.PublicationContent)] // Пустой Заголовок
    [InlineData(null, TestConstants.PublicationContent)] // Пустой Заголовок
    [InlineData("\t", TestConstants.PublicationContent)] // Пустой Заголовок
    [InlineData("ме", TestConstants.PublicationContent)] // Заголовок меньше 3 символов
    [InlineData(TestConstants.PublicationTitleMore64Chars, TestConstants.PublicationContent)] // Заголовок больше 64 символов

    [InlineData("Заголовок", "")] // Пустое Содержимое
    [InlineData("Заголовок", null)] // Пустое Содержимое
    [InlineData("Заголовок", "\t")] // Пустое Содержимое
    [InlineData("Заголовок", TestConstants.PublicationContentLess128Chars)] // Содержимое меньше 128 символов
    [InlineData("Заголовок", TestConstants.PublicationContentMore1024Chars)] // Содержимое больше 1024 символов

    [InlineData(null, null)] // Пустые данные
    public async Task ValidateAsync_NotValidData_ReturnsIsNotValid(string title, string content)
    {
        // Arrange
        var createPublicationDto = new CreatePublicationDto()
        {
            Title = title,
            Content = content
        };

        // Act
        var result = await _validator.ValidateAsync(createPublicationDto);

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
        CreatePublicationDto createPublicationDto = null;

        // Act
        Func<Task> a = async () =>
        {
            await _validator.ValidateAsync(createPublicationDto);
        };

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(a);
    }
}