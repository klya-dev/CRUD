namespace CRUD.Tests.UnitTests;

public class AuthRefreshTokenManagerUnitTest
{
    private readonly AuthRefreshTokenManager _authRefreshTokenManager;
    private readonly Mock<IValidator<AuthRefreshToken>> _mockAuthRefreshTokenValidator;
    private readonly Mock<IOptionsMonitor<AuthWebApiOptions>> _mockAuthWebApiOptions;
    private readonly ApplicationDbContext _db;

    public AuthRefreshTokenManagerUnitTest()
    {
        var db = DbContextGenerator.GenerateDbContextTestInMemory();
        _db = db;

        _mockAuthRefreshTokenValidator = new();
        _mockAuthWebApiOptions = new();

        _authRefreshTokenManager = new AuthRefreshTokenManager(
            db,
            _mockAuthRefreshTokenValidator.Object,
            _mockAuthWebApiOptions.Object
        );
    }

    [Fact]
    public async Task AddRefreshTokenAndDeleteOldersAsync_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        string newRefreshToken = null;
        Guid userIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _authRefreshTokenManager.AddRefreshTokenAndDeleteOldersAsync(newRefreshToken, userIdGuid);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(newRefreshToken), ex.ParamName);
    }

    [Fact]
    public async Task AddRefreshTokenAndDeleteOldersAsync_NotValidGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier()
    {
        // Arrange
        var refreshToken = "some";
        Guid userIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _authRefreshTokenManager.AddRefreshTokenAndDeleteOldersAsync(refreshToken, userIdGuid);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }
}