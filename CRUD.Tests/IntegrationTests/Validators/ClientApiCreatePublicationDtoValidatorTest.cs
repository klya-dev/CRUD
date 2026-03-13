#nullable disable
using CRUD;
using CRUD.Models.Validators;
using CRUD.Models.Validators.ValidatorsLocalizer;
using CRUD.Tests.Helpers;

namespace CRUD.Tests.IntegrationTests.Validators;

public class ClientApiCreatePublicationDtoValidatorTest
{
    // #nullable disable

    private readonly ValidatorsLocalizer _validatorsLocalizer;
    private readonly ClientApiCreatePublicationDtoValidator _validator;

    public ClientApiCreatePublicationDtoValidatorTest()
    {
        _validatorsLocalizer = new Models.Validators.ValidatorsLocalizer.ValidatorsLocalizer();
        _validator = new ClientApiCreatePublicationDtoValidator(_validatorsLocalizer);
    }

    [Theory]
    [InlineData("Заголовок", TestConstants.PublicationContent, TestConstants.UserApiKey)] // Корректные данные
    public async Task ValidateAsync_CorrectData_ReturnsIsValid(string title, string content, string apiKey)
    {
        // Arrange
        var clientApiCreatePublicationDto = new ClientApiCreatePublicationDto()
        {
            Title = title,
            Content = content,
            ApiKey = apiKey
        };

        // Act
        var result = await _validator.ValidateAsync(clientApiCreatePublicationDto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("", TestConstants.PublicationContent, TestConstants.UserApiKey)] // Пустой Заголовок
    [InlineData(null, TestConstants.PublicationContent, TestConstants.UserApiKey)] // Пустой Заголовок
    [InlineData("\t", TestConstants.PublicationContent, TestConstants.UserApiKey)] // Пустой Заголовок
    [InlineData("ме", TestConstants.PublicationContent, TestConstants.UserApiKey)] // Заголовок меньше 3 символов
    [InlineData(TestConstants.PublicationTitleMore64Chars, TestConstants.PublicationContent, TestConstants.UserApiKey)] // Заголовок больше 64 символов

    [InlineData("Заголовок", "", TestConstants.UserApiKey)] // Пустое Содержимое
    [InlineData("Заголовок", null, TestConstants.UserApiKey)] // Пустое Содержимое
    [InlineData("Заголовок", "\t", TestConstants.UserApiKey)] // Пустое Содержимое
    [InlineData("Заголовок", TestConstants.PublicationContentLess128Chars, TestConstants.UserApiKey)] // Содержимое меньше 128 символов
    [InlineData("Заголовок", TestConstants.PublicationContentMore1024Chars, TestConstants.UserApiKey)] // Содержимое больше 1024 символов

    [InlineData("Заголовок", TestConstants.PublicationContent, "")] // Пустой API-ключ
    [InlineData("Заголовок", TestConstants.PublicationContent, "\t")] // Пустой API-ключ
    [InlineData("Заголовок", TestConstants.PublicationContent, TestConstants.UserApiKeyLess100Chars)] // API-ключ меньше 100 символов
    [InlineData("Заголовок", TestConstants.PublicationContent, ".")] // API-ключ меньше 100 символов
    [InlineData("Заголовок", TestConstants.PublicationContent, TestConstants.UserApiKeyMore100Chars)] // API-ключ больше 100 символов
    [InlineData("Заголовок", TestConstants.PublicationContent, TestConstants.Spaces100)] //  API-ключ состоит из 100 пробелов

    [InlineData(null, null, null)] // Пустые данные
    public async Task ValidateAsync_NotValidData_ReturnsIsNotValid(string title, string content, string apiKey)
    {
        // Arrange
        var clientApiCreatePublicationDto = new ClientApiCreatePublicationDto()
        {
            Title = title,
            Content = content,
            ApiKey = apiKey
        };

        // Act
        var result = await _validator.ValidateAsync(clientApiCreatePublicationDto);

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
        ClientApiCreatePublicationDto clientApiCreatePublicationDto = null;

        // Act
        Func<Task> a = async () =>
        {
            await _validator.ValidateAsync(clientApiCreatePublicationDto);
        };

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(a);
    }
}