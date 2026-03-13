#nullable disable
using CRUD;
using CRUD.Models.Validators;

namespace CRUD.Tests.IntegrationTests.Validators.Publication;

public class GetPublicationsDtoValidatorTest
{
    // #nullable disable

    private readonly GetPublicationsDtoValidator _validator;

    public GetPublicationsDtoValidatorTest()
    {
        var validatorsLocalizer = new Models.Validators.ValidatorsLocalizer.ValidatorsLocalizer();
        _validator = new GetPublicationsDtoValidator(validatorsLocalizer);
    }

    [Theory]
    [InlineData(1)] // Корректные данные
    [InlineData(25)]
    [InlineData(100)]
    public async Task ValidateAsync_CorrectData_ReturnsIsValid(int count)
    {
        // Arrange
        var getPublicationsDto = new GetPublicationsDto()
        {
            Count = count
        };

        // Act
        var result = await _validator.ValidateAsync(getPublicationsDto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(0)] // Пустое количество
    [InlineData(-1)] // Меньше, чем 1
    [InlineData(101)] // Больше, чем 100
    public async Task ValidateAsync_NotValidData_ReturnsIsNotValid(int count)
    {
        // Arrange
        var getPublicationsDto = new GetPublicationsDto()
        {
            Count = count
        };

        // Act
        var result = await _validator.ValidateAsync(getPublicationsDto);

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
        GetPublicationsDto getPublicationsDto = null;

        // Act
        Func<Task> a = async () =>
        {
            await _validator.ValidateAsync(getPublicationsDto);
        };

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(a);
    }
}