#nullable disable
using CRUD.Models.Validators;

namespace CRUD.Tests.IntegrationTests.Validators.User;

public class UpdateUserDtoValidatorTest
{
    // #nullable disable

    private readonly UpdateUserDtoValidator _validator;

    public UpdateUserDtoValidatorTest()
    {
        var validatorsLocalizer = new Models.Validators.ValidatorsLocalizer.ValidatorsLocalizer();
        _validator = new UpdateUserDtoValidator(validatorsLocalizer);
    }

    [Theory]
    [InlineData("Имя", "username", "ru")] // Корректные данные
    [InlineData("Влад", "vladik", "tk")]
    [InlineData("жорж", "superjoj", "en")]
    public async Task ValidateAsync_CorrectData_ReturnsIsValid(string firstname, string username, string languageCode)
    {
        // Arrange
        var updateUserDto = new UpdateUserDto()
        {
            Firstname = firstname,
            Username = username,
            LanguageCode = languageCode
        };

        // Act
        var result = await _validator.ValidateAsync(updateUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("", "username", "ru")] // Пустое Имя
    [InlineData(null, "username", "ru")] // Пустое Имя
    [InlineData("\t", "username", "ru")] // Пустое имя
    [InlineData("Ж", "username", "ru")] // Имя меньше 2 символов
    [InlineData("ЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦ", "username", "ru")] // Имя больше 32 символов
    [InlineData("First123", "username", "ru")] // Имя содержит цифры
    [InlineData("имя@", "username", "ru")] // Имя содержит специальные символы
    [InlineData("name", "username", "ru")] // Имя из латиницы
    [InlineData("name_name", "username", "ru")] // Имя из латиницы
    [InlineData("Имя Два", "username", "ru")] // Имя с пробелом
    [InlineData("ИмяFirst", "username", "ru")] // Имя из кириллицы и латиницы

    [InlineData("Имя", "", "ru")] // Пустой Username
    [InlineData("Имя", null, "ru")] // Пустой Username
    [InlineData("Имя", "\t", "ru")] // Пустой Username
    [InlineData("Имя", "u", "ru")] // Username меньше 4 символов
    [InlineData("Имя", "zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz", "ru")] // Имя больше 32 символов
    [InlineData("Имя", "$username@", "ru")] // Username содержит специальные символы
    [InlineData("Имя", "юзернейм", "ru")] // Username из кириллицы
    [InlineData("Имя", "юзернейм_2", "ru")] // Username из кириллицы
    [InlineData("Имя", "username twice", "ru")] // Username с пробелом
    [InlineData("Имя", "юзерname", "ru")] // Username из кириллицы и латиницы

    [InlineData("Имя", "username", "")] // Пустой Language Code
    [InlineData("Имя", "username", null)] // Пустой Language Code
    [InlineData("Имя", "username", "\t")] // Пустой Language Code
    [InlineData("Имя", "username", "r")] // Language Code меньше 2 символов
    [InlineData("Имя", "username", "rus")] // Language Code больше 2 символов
    [InlineData("Имя", "username", "ru2")] // Language Code содержит цифры
    [InlineData("Имя", "username", "r2")] // Language Code содержит цифры
    [InlineData("Имя", "username", "ru@")] // Language Code содержит специальные символы
    [InlineData("Имя", "username", "u@")] // Language Code содержит специальные символы
    [InlineData("Имя", "username", "ру")] // Language Code из кириллицы
    [InlineData("Имя", "username", "р_у")] // Language Code из кириллицы
    [InlineData("Имя", "username", "р у")] // Language Code с пробелом
    [InlineData("Имя", "username", "жu")] // Language Code из кириллицы и латиницы

    [InlineData(null, null, null)] // Пустые данные

    [InlineData("", "", "ru")] // Пустое Имя и Username
    [InlineData("imya", "ggwwe", "ru")] // Невалидное Имя
    [InlineData("Имя", "supersus", "..")] // Невалидный Language Code
    [InlineData("Виталий", "юзер нэйм", "w")] // Невалидный Username и Language Code
    public async Task ValidateAsync_NotValidData_ReturnsIsNotValid(string firstname, string username, string languageCode)
    {
        // Arrange
        var updateUserDto = new UpdateUserDto()
        {
            Firstname = firstname,
            Username = username,
            LanguageCode = languageCode
        };

        // Act
        var result = await _validator.ValidateAsync(updateUserDto);

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
        UpdateUserDto updateUserDto = null;

        // Act
        Func<Task> a = async () =>
        {
            await _validator.ValidateAsync(updateUserDto);
        };

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(a);
    }
}