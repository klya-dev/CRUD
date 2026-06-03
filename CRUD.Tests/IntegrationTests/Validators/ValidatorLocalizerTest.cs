using CRUD;
using CRUD.Models.Validators.Localization.Languages;
using CRUD.Models.Validators.Localization;
using System.Globalization;

namespace CRUD.Tests.IntegrationTests.Validators;

public sealed class ValidatorLocalizerTest
{
    private readonly string[] _supportedCultures = ["ru", "en"];

    // Не хочется отдельно тестировать, один и тот же алгоритм для всех языков
    private static string GetTranslation(string key, string culture)
    {
        if (culture == "ru")
            return RussianValidatorLanguage.GetTranslation(key);
        else if (culture == "en")
            return EnglishValidatorLanguage.GetTranslation(key);

        // XLanguage.GetTranslation(name) в случае отсутствия ключа возвращает null, тут так же
        return null;
    }

    private static ValidatorLocalizer CreateValidatorLocalizer() => new();

    [Theory]
    [InlineData(ValidatorLocalizerConstants.OnlyCyrillic)]
    [InlineData(ValidatorLocalizerConstants.OnlySmallCaseLatin)]
    [InlineData(ValidatorLocalizerConstants.InvalidRole)]
    public void This_CorrectData_ReturnsString(string key)
    {
        foreach (var culture in _supportedCultures)
        {
            // Arrange
            CultureInfo.CurrentUICulture = new CultureInfo(culture);
            var localizer = CreateValidatorLocalizer();
            string localizeString = GetTranslation(key, culture); // Немного шиза, но я хочу удостоверится, чтобы я получаю задуманный перевод, причём используя, считай, ту же логику, которая описана в сервисе

            // Act
            var result = localizer[key];

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result, localizeString);
        }
    }

    [Theory]
    [InlineData("undefine key")]
    [InlineData("none")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData(" \t  \t ")]
    [InlineData(null)]
    public void This_WrongData_ReturnsNull(string key)
    {
        foreach (var culture in _supportedCultures)
        {
            // Arrange
            CultureInfo.CurrentUICulture = new CultureInfo(culture);
            var localizer = CreateValidatorLocalizer();

            // Act
            var result = localizer[key];

            // Assert
            Assert.Null(result);
        }
    }

    [Theory]
    [InlineData(ValidatorLocalizerConstants.TestParams, 30, "10")]
    public void ThisWithParams_CorrectData_ReturnsString(string key, params object[] args)
    {
        foreach (var culture in _supportedCultures)
        {
            // Arrange
            CultureInfo.CurrentUICulture = new CultureInfo(culture);
            var localizer = CreateValidatorLocalizer();
            var paramsStrings = args.Select(x => x.ToString()).ToList();
            var localizeString = localizer.ReplaceParams(key, paramsStrings!);

            // Act
            var result = localizer[key, args];

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result, localizeString);
        }
    }

    [Theory]
    [InlineData("undefine key", 30, "10")]
    [InlineData(null, "none")]
    public void ThisWithParams_WrongData_ThrowsArgumentException(string key, params object[] args)
    {
        foreach (var culture in _supportedCultures)
        {
            // Arrange
            CultureInfo.CurrentUICulture = new CultureInfo(culture);
            var localizer = CreateValidatorLocalizer();

            // Act
            Action a = () =>
            {
                var result = localizer[key, args];
            };

            var ex = Assert.Throws<ArgumentException>(a);

            // Assert
            Assert.Contains(ErrorMessages.KeyNotFound, ex.Message);
        }
    }

    [Theory]
    [InlineData("none", null)]
    [InlineData(null, null)]
    public void ThisWithParams_WrongData_ThrowsArgumentNullException(string key, params object[] args)
    {
        foreach (var culture in _supportedCultures)
        {
            // Arrange
            CultureInfo.CurrentUICulture = new CultureInfo(culture);
            var localizer = CreateValidatorLocalizer();

            // Act
            Action a = () =>
            {
                var result = localizer[key, args];
            };

            var ex = Assert.Throws<ArgumentNullException>(a);

            // Assert
            Assert.Contains(nameof(args), ex.ParamName); // Именно этот параметр вызвал исключение
        }
    }

    [Theory]
    [InlineData(ValidatorLocalizerConstants.OnlyCyrillic)]
    [InlineData(ValidatorLocalizerConstants.OnlySmallCaseLatin)]
    [InlineData(ValidatorLocalizerConstants.InvalidRole)]
    [InlineData("undefine key")]
    [InlineData("none")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData(" \t  \t ")]
    public void This_NotNullStringData_CurrentUICultureNotDefine_ReturnsName(string key)
    {
        // Arrange
        var localizer = CreateValidatorLocalizer();

        // Act
        var result = localizer[key];

        // Assert
        Assert.NotNull(result);
        Assert.Equal(result, key);
    }

    [Theory]
    [InlineData(ValidatorLocalizerConstants.OnlyCyrillic, 30, "10")]
    [InlineData(ValidatorLocalizerConstants.OnlySmallCaseLatin, 4.5)]
    [InlineData(ValidatorLocalizerConstants.InvalidRole, "")]
    [InlineData("undefine key")]
    [InlineData("none", "\t")]
    [InlineData(" ")]
    [InlineData("  ", "  ")]
    [InlineData("\t", "\t")]
    [InlineData(" \t  \t ", " \t  \t ")]
    [InlineData(" \t  \t ")]
    public void ThisWithParams_NotNullStringData_CurrentUICultureNotDefine_ReturnsName(string key, params object[] args)
    {
        // Arrange
        var localizer = CreateValidatorLocalizer();

        // Act
        var result = localizer[key, args];

        // Assert
        Assert.NotNull(result);
        Assert.Equal(result, key);
    }

    [Fact]
    public void This_NullObject_CurrentUICultureNotDefine_ReturnsNull()
    {
        // Arrange
        var localizer = CreateValidatorLocalizer();

        // Act
        var result = localizer[null];

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    public void ThisWithParams_NullObject_CurrentUICultureNotDefine_ReturnsNull(string key, params object[] args)
    {
        // Arrange
        var localizer = CreateValidatorLocalizer();

        // Act
        var result = localizer[key, args];

        // Assert
        Assert.Null(result);
        Assert.Equal(result, key);
    }

    [Theory]
    [InlineData(null, null)]
    public void ThisWithParams_NullObject_CurrentUICultureNotDefine_ThrowsArgumentNullException(string key, params object[] args)
    {
        // Arrange
        var localizer = CreateValidatorLocalizer();

        // Act
        Action a = () =>
        {
            var result = localizer[key, args];
        };

        var ex = Assert.Throws<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(args), ex.ParamName);
    }
}