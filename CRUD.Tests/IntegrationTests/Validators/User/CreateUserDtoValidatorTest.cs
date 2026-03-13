#nullable disable
using CRUD;
using CRUD.Models.Validators;
using CRUD.Tests.Helpers;

namespace CRUD.Tests.IntegrationTests.Validators.User;

public class CreateUserDtoValidatorTest
{
    // #nullable disable

    private readonly CreateUserDtoValidator _validator;

    public CreateUserDtoValidatorTest()
    {
        var validatorsLocalizer = new Models.Validators.ValidatorsLocalizer.ValidatorsLocalizer();
        _validator = new CreateUserDtoValidator(validatorsLocalizer);
    }

    [Theory]
    [InlineData("Имя", "username123-_", "password", "ru", "fan.jopa@mail.ru", "7999999")] // Корректные данные
    [InlineData("Витя", "vituatm", "123#$", "ru", "test@gmail.com", "12345")]
    [InlineData("Витя", "supername", "super_vitya224", "ru", "jojo@ya.ru", "52323123424")]
    public async Task ValidateAsync_CorrectData_ReturnsIsValid(string firstname, string username, string password, string languageCode, string email, string phoneNumber)
    {
        // Arrange
        var createUserDto = new CreateUserDto()
        {
            Firstname = firstname,
            Username = username,
            Password = password,
            LanguageCode = languageCode,
            Email = email,
            PhoneNumber = phoneNumber
        };

        // Act
        var result = await _validator.ValidateAsync(createUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("", "username", "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустое Имя
    [InlineData(null, "username", "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустое Имя
    [InlineData("\t", "username", "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустое имя
    [InlineData("Ж", "username", "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Имя меньше 2 символов
    [InlineData("ЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦ", "username", "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Имя больше 32 символов
    [InlineData("First123", "username", "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Имя содержит цифры
    [InlineData("имя@", "username", "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Имя содержит специальные символы
    [InlineData("name", "username", "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Имя из латиницы
    [InlineData("name_name", "username", "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Имя из латиницы
    [InlineData("Имя Два", "username", "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Имя с пробелом
    [InlineData("ИмяFirst", "username", "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Имя из кириллицы и латиницы

    [InlineData("Имя", "", "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой Username
    [InlineData("Имя", null, "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой Username
    [InlineData("Имя", "\t", "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой Username
    [InlineData("Имя", "u", "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Username меньше 4 символов
    [InlineData("Имя", "zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz", "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Имя больше 32 символов
    [InlineData("Имя", "$username@", "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Username содержит специальные символы
    [InlineData("Имя", "юзернейм", "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Username из кириллицы
    [InlineData("Имя", "юзернейм_2", "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Username из кириллицы
    [InlineData("Имя", "username twice", "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Username с пробелом
    [InlineData("Имя", "юзерname", "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Username из кириллицы и латиницы

    [InlineData("Имя", "username", "", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой Пароль
    [InlineData("Имя", "username", null, "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой Пароль
    [InlineData("Имя", "username", "\t", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой Пароль

    [InlineData("Имя", "username", "password", "", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой Language Code
    [InlineData("Имя", "username", "password", null, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой Language Code
    [InlineData("Имя", "username", "password", "\t", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой Language Code
    [InlineData("Имя", "username", "password", "r", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Language Code меньше 2 символов
    [InlineData("Имя", "username", "password", "rus", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Language Code больше 2 символов
    [InlineData("Имя", "username", "password", "ru2", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Language Code содержит цифры
    [InlineData("Имя", "username", "password", "r2", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Language Code содержит цифры
    [InlineData("Имя", "username", "password", "ru@", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Language Code содержит специальные символы
    [InlineData("Имя", "username", "password", "u@", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Language Code содержит специальные символы
    [InlineData("Имя", "username", "password", "ру", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Language Code из кириллицы
    [InlineData("Имя", "username", "password", "р_у", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Language Code из кириллицы
    [InlineData("Имя", "username", "password", "р у", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Language Code с пробелом
    [InlineData("Имя", "username", "password", "жu", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Language Code из кириллицы и латиницы

    [InlineData(null, null, null, null, null, null)] // Пустые данные

    [InlineData("Имя", "username", "password", "ru", "", TestConstants.UserPhoneNumber)] // Пустой Email
    [InlineData("Имя", "username", "password", "ru", null, TestConstants.UserPhoneNumber)] // Пустой Email
    [InlineData("Имя", "username", "password", "ru", "\t", TestConstants.UserPhoneNumber)] // Пустой Email
    [InlineData("Имя", "username", "password", "ru", "@", TestConstants.UserPhoneNumber)] // Email только из @
    [InlineData("Имя", "username", "password", "ru", "a@m.r", TestConstants.UserPhoneNumber)] // Email меньше 6 символов
    [InlineData("Имя", "username", "password", "ru", TestConstants.UserEmailMore254Chars, TestConstants.UserPhoneNumber)] // Email больше 254 символов
    [InlineData("Имя", "username", "password", "ru", "почта@е.ру", TestConstants.UserPhoneNumber)] // Email из кириллицы
    [InlineData("Имя", "username", "password", "ru", "test@mail.ru ", TestConstants.UserPhoneNumber)] // Email с пробелом
    [InlineData("Имя", "username", "password", "ru", "pochta@маил.ру", TestConstants.UserPhoneNumber)] // Email из кириллицы и латиницы

    [InlineData("Имя", "username", "password", "ru", TestConstants.UserEmail, "")] // Пустой Phone Number
    [InlineData("Имя", "username", "password", "ru", TestConstants.UserEmail, null)] // Пустой Phone Number
    [InlineData("Имя", "username", "password", "ru", TestConstants.UserEmail, "\t")] // Пустой Phone Number
    [InlineData("Имя", "username", "password", "ru", TestConstants.UserEmail, "123")] // Phone Number меньше 5 символов
    [InlineData("Имя", "username", "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumberMore15Chars)] // Phone Number больше 15 символов
    [InlineData("Имя", "username", "password", "ru", TestConstants.UserEmail, "номер")] // Phone Number из кириллицы
    [InlineData("Имя", "username", "password", "ru", TestConstants.UserEmail, "+7999999")] // Phone Number с плюсом
    [InlineData("Имя", "username", "password", "ru", TestConstants.UserEmail, "799Ж9999")] // Phone Number из кириллицы и цифр

    [InlineData("", null, "password", "ru", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустое Имя и Username
    [InlineData("Таракан", "nikstas", "", "www", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Невалидный Пароль и Language Code
    [InlineData("Александр", "sasha228@", "qerqw12@#!", "tr", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Невалидный Username
    public async Task ValidateAsync_NotValidData_ReturnsIsNotValid(string firstname, string username, string password, string languageCode, string email, string phoneNumber)
    {
        // Arrange
        var createUserDto = new CreateUserDto()
        {
            Firstname = firstname,
            Username = username,
            Password = password,
            LanguageCode = languageCode,
            Email = email,
            PhoneNumber = phoneNumber
        };

        // Act
        var result = await _validator.ValidateAsync(createUserDto);

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
        CreateUserDto createUserDto = null;

        // Act
        Func<Task> a = async () =>
        {
            await _validator.ValidateAsync(createUserDto);
        };

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(a);
    }
}