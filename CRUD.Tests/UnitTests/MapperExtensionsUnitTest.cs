namespace CRUD.Tests.UnitTests;

public class MapperExtensionsUnitTest
{
    public MapperExtensionsUnitTest()
    {

    }

    [Fact]
    public void ToUserDto_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        User user = null;

        // Act
        Action a = () =>
        {
            Models.MapperExtensions.ToUserDto(user);
        };

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(a);
        Assert.Contains(nameof(user), ex.ParamName);
    }


    [Fact]
    public void ToUsersDto_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<User> users = null;

        // Act
        Action a = () =>
        {
            Models.MapperExtensions.ToUsersDto(users);
        };

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(a);
        Assert.Contains(nameof(users), ex.ParamName);
    }


    [Fact]
    public void ToPublicationDto_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        Publication publication = null;

        // Act
        Action a = () =>
        {
            Models.MapperExtensions.ToPublicationDto(publication, "");
        };

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(a);
        Assert.Contains(nameof(publication), ex.ParamName);
    }


    [Fact]
    public void ToPublicationsDto_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<Publication> publications = null;

        // Act
        Action a = () =>
        {
            Models.MapperExtensions.ToPublicationsDto(publications, "");
        };

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(a);
        Assert.Contains(nameof(publications), ex.ParamName);
    }


    [Fact]
    public void ToPublicationsDtoByFunc_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<Publication> publications = null;

        // Act
        Action a = () =>
        {
            Models.MapperExtensions.ToPublicationsDto(publications, x => x.User?.Firstname);
        };

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(a);
        Assert.Contains(nameof(publications), ex.ParamName);
    }


    [Fact]
    public void ToWithoutTicks_ReturnsDatetimeWithoutTicks()
    {
        // Arrange
        DateTime dateTime = DateTime.Now;

        // Act
        var result = dateTime.ToWithoutTicks();

        // Assert
        Assert.Equal(dateTime.Year, result.Year);
        Assert.Equal(dateTime.Month, result.Month);
        Assert.Equal(dateTime.Day, result.Day);
        Assert.Equal(dateTime.Hour, result.Hour);
        Assert.Equal(dateTime.Minute, result.Minute);
        Assert.Equal(dateTime.Second, result.Second);

        Assert.Equal(0, result.Millisecond);
        Assert.Equal(0, result.Microsecond);
        Assert.Equal(0, result.Nanosecond);
    }

    [Fact]
    public void ToWithoutTicks_Utc_ReturnsDatetimeWithoutTicks()
    {
        // Arrange
        DateTime dateTime = DateTime.UtcNow;

        // Act
        var result = dateTime.ToWithoutTicks();

        // Assert
        Assert.Equal(dateTime.Year, result.Year);
        Assert.Equal(dateTime.Month, result.Month);
        Assert.Equal(dateTime.Day, result.Day);
        Assert.Equal(dateTime.Hour, result.Hour);
        Assert.Equal(dateTime.Minute, result.Minute);
        Assert.Equal(dateTime.Second, result.Second);

        Assert.Equal(0, result.Millisecond);
        Assert.Equal(0, result.Microsecond);
        Assert.Equal(0, result.Nanosecond);
    }


    [Fact]
    public void ToCreateUserDto_ReturnsCreateUserDto()
    {
        // Arrange
        var userInfo = new OpenIdUserInfo
        {
            Sub = "123",
            Name = "фантом ассасин",
            GivenName = "фантом",
            FamilyName = "ассасин",
            Nickname = "assasin",
            Picture = "some",
            Gender = "male",
            Birthdate = DateTime.Now,
            Locale = "ru",
            Email = "some@some.some"
        };

        var oAuthCompleteRegistrationDto = new OAuthCompleteRegistrationDto
        {
            PhoneNumber = "123456789"
        };

        // Act
        var result = userInfo.ToCreateUserDto(oAuthCompleteRegistrationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userInfo.GivenName, result.Firstname);
        Assert.Equal(userInfo.Nickname, result.Username);
        Assert.NotNull(result.Password);
        Assert.Equal(userInfo.Locale, result.LanguageCode);
        Assert.Equal(userInfo.Email, result.Email);
        Assert.Equal(oAuthCompleteRegistrationDto.PhoneNumber, result.PhoneNumber);
    }

    [Fact]
    public void ToCreateUserDto_WhenGivenNameIsNotCyrillic_ReturnsCreateUserDto()
    {
        // Arrange
        var userInfo = new OpenIdUserInfo
        {
            Sub = "123",
            Name = "фантом ассасин",
            GivenName = "SOME", // Не кириллица
            FamilyName = "ассасин",
            Nickname = "assasin",
            Picture = "some",
            Gender = "male",
            Birthdate = DateTime.Now,
            Locale = "ru",
            Email = "some@some.some"
        };

        var oAuthCompleteRegistrationDto = new OAuthCompleteRegistrationDto
        {
            PhoneNumber = "123456789"
        };

        // Act
        var result = userInfo.ToCreateUserDto(oAuthCompleteRegistrationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Неизвестно", result.Firstname);
        Assert.Equal(userInfo.Nickname, result.Username);
        Assert.NotNull(result.Password);
        Assert.Equal(userInfo.Locale, result.LanguageCode);
        Assert.Equal(userInfo.Email, result.Email);
        Assert.Equal(oAuthCompleteRegistrationDto.PhoneNumber, result.PhoneNumber);
    }

    [Fact]
    public void ToCreateUserDto_WhenLocaleMore2Chars_ReturnsCreateUserDto()
    {
        // Arrange
        var userInfo = new OpenIdUserInfo
        {
            Sub = "123",
            Name = "фантом ассасин",
            GivenName = "фантом",
            FamilyName = "ассасин",
            Nickname = "assasin",
            Picture = "some",
            Gender = "male",
            Birthdate = DateTime.Now,
            Locale = "rus", // Больше двух символов
            Email = "some@some.some"
        };

        var oAuthCompleteRegistrationDto = new OAuthCompleteRegistrationDto
        {
            PhoneNumber = "123456789"
        };

        // Act
        var result = userInfo.ToCreateUserDto(oAuthCompleteRegistrationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userInfo.GivenName, result.Firstname);
        Assert.Equal(userInfo.Nickname, result.Username);
        Assert.NotNull(result.Password);
        Assert.Equal("ru", result.LanguageCode);
        Assert.Equal(userInfo.Email, result.Email);
        Assert.Equal(oAuthCompleteRegistrationDto.PhoneNumber, result.PhoneNumber);
    }

    [Fact]
    public void ToCreateUserDto_WhenNicknameIsNotLatinReturnsCreateUserDto()
    {
        // Arrange
        var userInfo = new OpenIdUserInfo
        {
            Sub = "123",
            Name = "фантом ассасин",
            GivenName = "фантом",
            FamilyName = "ассасин",
            Nickname = "ассасин", // Не латиница
            Picture = "some",
            Gender = "male",
            Birthdate = DateTime.Now,
            Locale = "ru",
            Email = "some@some.some"
        };

        var oAuthCompleteRegistrationDto = new OAuthCompleteRegistrationDto
        {
            PhoneNumber = "123456789"
        };

        // Act
        var result = userInfo.ToCreateUserDto(oAuthCompleteRegistrationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userInfo.GivenName, result.Firstname);
        Assert.StartsWith("und-", result.Username);
        Assert.NotNull(result.Password);
        Assert.Equal(userInfo.Locale, result.LanguageCode);
        Assert.Equal(userInfo.Email, result.Email);
        Assert.Equal(oAuthCompleteRegistrationDto.PhoneNumber, result.PhoneNumber);
    }
}