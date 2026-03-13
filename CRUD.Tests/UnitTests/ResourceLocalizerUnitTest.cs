#nullable disable
using Microsoft.Extensions.Localization;

namespace CRUD.Tests.UnitTests;

public class ResourceLocalizerUnitTest
{
    // #nullable disable

    private readonly Mock<IStringLocalizer> _localizerMock;
    private readonly Mock<IStringLocalizerFactory> _localizerFactoryMock;

    public ResourceLocalizerUnitTest()
    {
        _localizerMock = new();
        _localizerFactoryMock = new();
    }

    [Theory]
    [InlineData(ResourceLocalizerConstants.IncorrectRequestDetail, "Некорретный запрос.")]
    public void Localize_CorrectData_RU_ReturnsString(string original, string localizedOriginal)
    {
        // Arrange
        // Настройка локализатора
        _localizerMock.Setup(x => x[It.IsAny<string>()]).Returns(new LocalizedString(original, localizedOriginal));

        // Настройка фабрики локализаторов
        _localizerFactoryMock
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(_localizerMock.Object);

        ResourceLocalizer resourceLocalizer = new(_localizerFactoryMock.Object);

        // Act
        var result = resourceLocalizer[original];

        // Assert
        Assert.Equal(localizedOriginal, result);
    }

    [Theory]
    [InlineData(ResourceLocalizerConstants.IncorrectRequestDetail, "Incorrect request.")]
    public void Localize_CorrectData_EN_ReturnsString(string original, string localizedOriginal)
    {
        // Arrange
        // Настройка локализатора
        _localizerMock.Setup(x => x[It.IsAny<string>()]).Returns(new LocalizedString(original, localizedOriginal));

        // Настройка фабрики локализаторов
        _localizerFactoryMock
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(_localizerMock.Object);

        ResourceLocalizer resourceLocalizer = new(_localizerFactoryMock.Object);

        // Act
        var result = resourceLocalizer[original];

        // Assert
        Assert.Equal(localizedOriginal, result);
    }

    // Неизвестные ключи, которые не определены в переводах
    // Как итог, возвращается тот же ключ, вместо перевода
    [Theory]
    [InlineData("unknown key")]
    [InlineData("")]
    [InlineData("ResourceLocalizerConstants.IncorrectRequestDetail")]
    public void Localize_WrongData_ReturnsString(string original)
    {
        // Arrange
        // Настройка локализатора
        _localizerMock.Setup(x => x[It.IsAny<string>()]).Returns(new LocalizedString(original, original));

        // Настройка фабрики локализаторов
        _localizerFactoryMock
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(_localizerMock.Object);

        ResourceLocalizer resourceLocalizer = new(_localizerFactoryMock.Object);

        // Act
        var result = resourceLocalizer[original];

        // Assert
        Assert.Equal(original, result);
    }

    [Theory]
    [InlineData(null)]
    public void Localize_NullObject_ThrowsArgumentNullException(string original)
    {
        // Arrange
        // Настройка локализатора
        _localizerMock.Setup(x => x[It.IsAny<string>()]).Throws(new ArgumentNullException(original));

        // Настройка фабрики локализаторов
        _localizerFactoryMock
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(_localizerMock.Object);

        ResourceLocalizer resourceLocalizer = new(_localizerFactoryMock.Object);

        // Act
        Action a = () =>
        {
            var result = resourceLocalizer[original];
        };

        Assert.Throws<ArgumentNullException>(a);
    }

    [Theory]
    [InlineData(ResourceLocalizerConstants.TestParams, "30", "10", "1")]
    public void ReplaceParams_CorrectData_ReturnsVoid(string original, params string[] args)
    {
        // Arrange
        var localizedOriginal = $"$A$ меньше, чем $B$, но больше, чем $C$"; // Что должен вернуть _localizer[name]
        var rightResult = $"{args[0]} меньше, чем {args[1]}, но больше, чем {args[2]}"; // Правильная локализация с заменой аргументов

        // Настройка локализатора
        _localizerMock.Setup(x => x[It.IsAny<string>()]).Returns(new LocalizedString(original, localizedOriginal));

        // Настройка фабрики локализаторов
        _localizerFactoryMock
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(_localizerMock.Object);

        ResourceLocalizer resourceLocalizer = new(_localizerFactoryMock.Object);

        // Act
        var result = resourceLocalizer.ReplaceParams(original, args.ToList());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(result, rightResult);
    }

    [Theory]
    [InlineData("undefine key", "30", "10")]
    [InlineData(null, "none")]
    [InlineData("none", null)]
    [InlineData(null, null)]
    public void ReplaceParams_WrongData_ThrowsArgumentNullException(string original, params string[] args)
    {
        // Arrange
        // Настройка локализатора
        _localizerMock.Setup(x => x[It.IsAny<string>()]).Throws(new ArgumentNullException());

        // Настройка фабрики локализаторов
        _localizerFactoryMock
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(_localizerMock.Object);

        ResourceLocalizer resourceLocalizer = new(_localizerFactoryMock.Object);

        // Act
        Action a = () =>
        {
            var result = resourceLocalizer.ReplaceParams(original, args.ToList());
        };

        // Assert
        Assert.Throws<ArgumentNullException>(a);
    }

    //[Theory]
    //[InlineData(LocalizerValidatorsConstants.OnlyCyrillic, "30", "10")]
    //[InlineData(LocalizerValidatorsConstants.OnlySmallCaseLatin, "4.5")]
    //[InlineData(LocalizerValidatorsConstants.InvalidRole, "")]
    //[InlineData("undefine key")]
    //[InlineData("none", "\t")]
    //[InlineData(" ")]
    //[InlineData("  ", "  ")]
    //[InlineData("\t", "\t")]
    //[InlineData(" \t  \t ", " \t  \t ")]
    //[InlineData(" \t  \t ")]
    //public void ReplaceParams_NotNullStringData_CurrentUICultureNotDefine_ReturnsName(string original, params string[] args)
    //{
    //    // Arrange
    //    CultureInfo.CurrentUICulture = new CultureInfo("");
    //    var localizerMock = new Mock<IStringLocalizer>();
    //    var localizerFactoryMock = new Mock<IStringLocalizerFactory>();

    //    // Настройка локализатора
    //    localizerMock.Setup(x => x[It.IsAny<string>()]).Returns(new LocalizedString(original, original));

    //    // Настройка фабрики локализаторов
    //    localizerFactoryMock
    //        .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>()))
    //        .Returns(localizerMock.Object);

    //    WebApi.ResourceLocalizer.ResourceLocalizer resourceLocalizer = new(localizerFactoryMock.Object);

    //    // Act
    //    var result = resourceLocalizer.ReplaceParams(original, args.ToList());

    //    // Assert
    //    Assert.NotNull(result);
    //    Assert.Equal(result, original);
    //}

    //[Theory]
    //[InlineData(null)]
    //public void ReplaceParams_NullObject_CurrentUICultureNotDefine_ReturnsNull(string original, params string[] args)
    //{
    //    // Arrange
    //    CultureInfo.CurrentUICulture = new CultureInfo("");
    //    var localizerMock = new Mock<IStringLocalizer>();
    //    var localizerFactoryMock = new Mock<IStringLocalizerFactory>();

    //    // Настройка локализатора
    //    localizerMock.Setup(x => x[It.IsAny<string>()]).Returns(() => null);

    //    // Настройка фабрики локализаторов
    //    localizerFactoryMock
    //        .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>()))
    //        .Returns(localizerMock.Object);

    //    WebApi.ResourceLocalizer.ResourceLocalizer resourceLocalizer = new(localizerFactoryMock.Object);

    //    // Act
    //    var result = resourceLocalizer.ReplaceParams(original, args.ToList());

    //    // Assert
    //    Assert.Null(result);
    //    Assert.Equal(result, original);
    //}

    //[Theory]
    //[InlineData(null, null)]
    //public void ReplaceParams_NullObject_CurrentUICultureNotDefine_ThrowsArgumentNullException(string original, params string[] args)
    //{
    //    // Arrange
    //    var localizer = CreateValidatorsLocalizer();

    //    // Act
    //    Action a = () =>
    //    {
    //        var result = localizer[name, objects];
    //    };

    //    // Assert
    //    Assert.Throws<ArgumentNullException>(a);
    //}
}