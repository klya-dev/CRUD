#nullable disable
using CRUD;
using CRUD.WebApi.ApiError;

namespace CRUD.Tests.IntegrationTests;

public class ApiErrorConstantsIntegrationTest
{
    // #nullable disable

    // Неотъемлемая часть эндпоинта, тщательно тестируем
    // Тут протестировали, значит в эндпоинтах не нужно тестировать "необработанную ошибку". Т.к всё равно сводится к методу Match

    [Theory]
    [InlineData(ErrorMessages.AuthorNotFound)]
    public void Match_CorrectData_ReturnsString(string errorMessageFromService)
    {
        // Arrange

        // Act
        var result = ApiErrorConstants.Match(errorMessageFromService);

        // Assert
        Assert.Equivalent(ApiErrorConstants.AuthorNotFound, result);
    }

    [Theory] // Необработаная ошибка сервиса
    [InlineData("НЕТ ТАКОЙ ОШИБКИ")]
    [InlineData("")]
    public void Match_UnhandledError_ThrowsInvalidOperationException(string errorMessageFromService)
    {
        // Arrange

        // Act
        Action a = () =>
        {
            ApiErrorConstants.Match(errorMessageFromService);
        };

        var ex = Assert.Throws<InvalidOperationException>(a);

        // Assert
        Assert.Contains("Raw outcome: " + errorMessageFromService, ex.Message);
    }

    [Fact]
    public void Match_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        string errorMessageFromService = null;

        // Act
        Action a = () =>
        {
            ApiErrorConstants.Match(errorMessageFromService);
        };

        var ex = Assert.Throws<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(errorMessageFromService), ex.ParamName);
    }
}