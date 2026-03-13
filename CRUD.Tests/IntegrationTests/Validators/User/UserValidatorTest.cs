#nullable disable
using CRUD;
using CRUD.Models.Validators;
using CRUD.Tests.Helpers;

namespace CRUD.Tests.IntegrationTests.Validators.User;

public class UserValidatorTest
{
    // #nullable disable

    private readonly UserValidator _validator;

    public UserValidatorTest()
    {
        _validator = new UserValidator();
    }

    [Theory]
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Корректные данные
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, null, TestConstants.UserEmail, TestConstants.UserPhoneNumber)]
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, null, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)]
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, null, null, TestConstants.UserEmail, TestConstants.UserPhoneNumber)]
    public async Task ValidateAsync_CorrectData_ReturnsIsValid(string firstname, string username, string hashedPassword, string languageCode, string role, bool isPremium, string apiKey, string disposableApiKey, string email, string phoneNumber)
    {
        // Arrange
        var user = new Models.Domains.User()
        {
            Firstname = firstname,
            Username = username,
            HashedPassword = hashedPassword,
            LanguageCode = languageCode,
            Role = role,
            IsPremium = isPremium,
            AvatarURL = TestConstants.DefaultAvatarPath,
            ApiKey = apiKey,
            DisposableApiKey = disposableApiKey,
            Email = email,
            PhoneNumber = phoneNumber
        };

        // Act
        var result = await _validator.ValidateAsync(user);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустое Имя
    [InlineData(null, "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустое Имя
    [InlineData("\t", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустое имя
    [InlineData("Ж", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Имя меньше 2 символов
    [InlineData("ЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦЦ", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Имя больше 32 символов
    [InlineData("First123", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Имя содержит цифры
    [InlineData("имя@", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Имя содержит специальные символы
    [InlineData("name", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Имя из латиницы
    [InlineData("name_name", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Имя из латиницы
    [InlineData("Имя Два", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Имя с пробелом
    [InlineData("ИмяFirst", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Имя из кириллицы и латиницы

    [InlineData("Имя", "", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой Username
    [InlineData("Имя", null, TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой Username
    [InlineData("Имя", "\t", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой Username
    [InlineData("Имя", "u", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Username меньше 4 символов
    [InlineData("Имя", "zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Имя больше 32 символов
    [InlineData("Имя", "$username@", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Username содержит специальные символы
    [InlineData("Имя", "юзернейм", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Username из кириллицы
    [InlineData("Имя", "юзернейм_2", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Username из кириллицы
    [InlineData("Имя", "username twice", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Username с пробелом
    [InlineData("Имя", "юзерname", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Username из кириллицы и латиницы

    [InlineData("Имя", "username", "", "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой Hashed Password
    [InlineData("Имя", "username", null, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой Hashed Password
    [InlineData("Имя", "username", "\t", "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой Hashed Password
    [InlineData("Имя", "username", "a", "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Hashed Password меньше 69 символов
    [InlineData("Имя", "username", "U=-vuljV612KMRwiW3qHni8iA==", "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Hashed Password меньше 69 символов
    [InlineData("Имя", "username", TestConstants.UserHashedPassword + "1", "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Hashed Password больше 69 символов

    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой Language Code
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, null, UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой Language Code
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "\t", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой Language Code
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "r", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Language Code меньше 2 символов
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "rus", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Language Code больше 2 символов
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru2", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Language Code содержит цифры
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "r2", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Language Code содержит цифры
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru@", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Language Code содержит специальные символы
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "u@", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Language Code содержит специальные символы
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ру", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Language Code из кириллицы
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "р_у", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Language Code из кириллицы
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "р у", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Language Code с пробелом
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "жu", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Language Code из кириллицы и латиницы

    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", "", true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустая Роль
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", null, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустая Роль
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", "\t", true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустая Роль
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", "InvalidRole", true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Несуществующая Роль
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", "SuperAdmin", true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Несуществующая Роль

    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, "", TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой API-ключ
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, "\t", TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой API-ключ
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKeyLess100Chars, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // API-ключ меньше 100 символов
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, ".", TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // API-ключ меньше 100 символов
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKeyMore100Chars, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // API-ключ больше 100 символов
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.Spaces100, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] //  API-ключ состоит из 100 пробелов

    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, "", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой Disposable API-ключ
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, "\t", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустой Disposable API-ключ
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKeyLess100Chars, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Disposable API-ключ меньше 100 символов
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, ".", TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Disposable API-ключ меньше 100 символов
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKeyMore100Chars, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Disposable API-ключ больше 100 символов
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.Spaces100, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Disposable API-ключ состоит из 100 пробелов

    [InlineData(null, null, null, null, null, true, null, null, null, null)] // Пустые данные
    [InlineData(null, null, null, null, null, false, null, null, null, null)] // Пустые данные

    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, "", TestConstants.UserPhoneNumber)] // Пустой Email
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, null, TestConstants.UserPhoneNumber)] // Пустой Email
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, "\t", TestConstants.UserPhoneNumber)] // Пустой Email
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, "@", TestConstants.UserPhoneNumber)] // Email только из @
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, "a@m.r", TestConstants.UserPhoneNumber)] // Email меньше 6 символов
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmailMore254Chars, TestConstants.UserPhoneNumber)] // Email больше 254 символов
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, "почта@е.ру", TestConstants.UserPhoneNumber)] // Email из кириллицы
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, "test@mail.ru ", TestConstants.UserPhoneNumber)] // Email с пробелом
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, "pochta@маил.ру", TestConstants.UserPhoneNumber)] // Email из кириллицы и латиницы

    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, "")] // Пустой Phone Number
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, null)] // Пустой Phone Number
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, "\t")] // Пустой Phone Number
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, "123")] // Phone Number меньше 5 символов
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumberMore15Chars)] // Phone Number больше 15 символов
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, "номер")] // Phone Number из кириллицы
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, "+7999999")] // Phone Number с плюсом
    [InlineData("Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, "799Ж9999")] // Phone Number из кириллицы и цифр

    [InlineData("", null, TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Пустое Имя и Username
    [InlineData("Вася", "vasmontenegro", TestConstants.UserHashedPassword, "ru", "VasyaRole", true, TestConstants.Spaces100, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Невалидная Роль и API-ключ
    [InlineData("Гриша", "greg", "qwerty100", "usa", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Невалидный Hashed Password и Language Code
    public async Task ValidateAsync_NotValidData_ReturnsIsNotValid(string firstname, string username, string hashedPassword, string languageCode, string role, bool isPremium, string apiKey, string disposableApiKey, string email, string phoneNumber)
    {
        // Arrange
        var user = new Models.Domains.User()
        {
            Firstname = firstname,
            Username = username,
            HashedPassword = hashedPassword,
            LanguageCode = languageCode,
            Role = role,
            IsPremium = isPremium,
            AvatarURL = TestConstants.DefaultAvatarPath,
            ApiKey = apiKey,
            DisposableApiKey = disposableApiKey,
            Email = email,
            PhoneNumber = phoneNumber
        };

        // Act
        var result = await _validator.ValidateAsync(user);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.NotEmpty(result.Errors);
    }

    [Theory]
    [InlineData(TestConstants.EmptyGuidString, "Имя", "username", TestConstants.UserHashedPassword, "ru", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Невалидный GUID
    [InlineData(TestConstants.EmptyGuidString, "Гриша", "greg", "qwerty100", "usa", UserRoles.Admin, true, TestConstants.UserApiKey, TestConstants.UserDisposableApiKey, TestConstants.UserEmail, TestConstants.UserPhoneNumber)] // Невалидный GUID, Hashed Password и Language Code
    public async Task ValidateAsync_NotValidGuid_ReturnsIsNotValid(string userId, string firstname, string username, string hashedPassword, string languageCode, string role, bool isPremium, string apiKey, string disposableApiKey, string email, string phoneNumber)
    {
        // Arrange
        var user = new Models.Domains.User()
        {
            Id = Guid.Parse(userId),
            Firstname = firstname,
            Username = username,
            HashedPassword = hashedPassword,
            LanguageCode = languageCode,
            Role = role,
            IsPremium = isPremium,
            AvatarURL = TestConstants.DefaultAvatarPath,
            ApiKey = apiKey,
            DisposableApiKey = disposableApiKey,
            Email = email,
            PhoneNumber = phoneNumber
        };

        // Act
        var result = await _validator.ValidateAsync(user);

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
        Models.Domains.User user = null;

        // Act
        Func<Task> a = async () =>
        {
            await _validator.ValidateAsync(user);
        };

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(a);
    }
}