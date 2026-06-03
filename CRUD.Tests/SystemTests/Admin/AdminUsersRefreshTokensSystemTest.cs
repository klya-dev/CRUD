using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CRUD.Tests.SystemTests.Admin;

public sealed class AdminUsersRefreshTokensSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;

    public AdminUsersRefreshTokensSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        // Добавляем Refresh-токены в базу
        var authRefreshToken = await DI.CreateAuthRefreshTokenAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);
        var authRefreshToken2 = await DI.CreateAuthRefreshTokenAsync(_db, userIdGuid, token: "12133244", ct: TestContext.Current.CancellationToken);

        // Запрос
        var url = string.Format(TestConstants.ADMIN_USERS_USER_ID_REFRESH_TOKENS_URL, user.Id);
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Все токены пользователя удалены
        var countAuthRefreshTokens = await _db.AuthRefreshTokens.Where(x => x.UserId == userIdGuid).CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, countAuthRefreshTokens);
    }

    [Fact]
    public async Task Delete_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        var userIdGuid = Guid.NewGuid();

        // Запрос
        var url = string.Format(TestConstants.ADMIN_USERS_USER_ID_REFRESH_TOKENS_URL, userIdGuid);
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.USER_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }
}