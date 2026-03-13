#nullable disable
namespace CRUD.Tests.IntegrationTests.Validators.Publication;

public class PublicationValidatorTest
{
    // #nullable disable

    private readonly PublicationValidator _validator;

    public PublicationValidatorTest()
    {
        _validator = new PublicationValidator();
    }

    [Theory]
    [InlineData("Заголовок", TestConstants.PublicationContent)] // Корректные данные
    [InlineData("Лето 2025", "Я провёл лето, так\n\nЛялялялялЛялялялялЛялялялялЛялялялялЛялялялялЛялялялялЛялялялялЛялялялялЛялялялялЛялялялялЛялялялялЛялялялял")]
    public async Task ValidateAsync_CorrectData_ReturnsIsValid(string title, string content)
    {
        // Arrange
        var publication = new Models.Domains.Publication()
        {
            Title = title,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            AuthorId = Guid.NewGuid(),
        };

        // Act
        var result = await _validator.ValidateAsync(publication);

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
    //[InlineData("Title", Content)] // Заголовок не из кириллицы
    //[InlineData("Заголовок2", Content)] // Заголовок не из кириллицы
    //[InlineData("Заголовок@", Content)] // Заголовок не из кириллицы

    [InlineData("Заголовок", "")] // Пустое Содержимое
    [InlineData("Заголовок", null)] // Пустое Содержимое
    [InlineData("Заголовок", "\t")] // Пустое Содержимое
    [InlineData("Заголовок", TestConstants.PublicationContentLess128Chars)] // Содержимое меньше 128 символов
    [InlineData("Заголовок", TestConstants.PublicationContentMore1024Chars)] // Содержимое больше 1024 символов

    [InlineData(null, null)] // Пустые данные
    public async Task ValidateAsync_NotValidData_ReturnsIsNotValid(string title, string content)
    {
        // Arrange
        var publication = new Models.Domains.Publication()
        {
            Title = title,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            AuthorId = Guid.NewGuid(),
        };

        // Act
        var result = await _validator.ValidateAsync(publication);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_NotValidDate_ReturnsIsNotValid()
    {
        // Arrange
        var publication = new Models.Domains.Publication()
        {
            Title = "title",
            Content = TestConstants.PublicationContent,
            CreatedAt = DateTime.MinValue,
            AuthorId = Guid.NewGuid(),
        };

        // Act
        var result = await _validator.ValidateAsync(publication);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.NotEmpty(result.Errors);
    }

    [Theory]
    [InlineData("Заголовок", TestConstants.PublicationContent)] // Корректные данные
    public async Task ValidateAsync_NotValidAuthor_ReturnsIsNotValid(string title, string content)
    {
        // Arrange
        var publication = new Models.Domains.Publication()
        {
            Title = title,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            AuthorId = Guid.Empty,
        };

        // Act
        var result = await _validator.ValidateAsync(publication);

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
        Models.Domains.Publication publication = null;

        // Act
        Func<Task> a = async () =>
        {
            await _validator.ValidateAsync(publication);
        };

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(a);
    }
}