using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CRUD.Tests.SystemTests.Admin;

public class AdminUsersPremiumSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;

    public AdminUsersPremiumSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
    }

    [Fact]
    public async Task Put_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: false);
        
        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);

        // Запрос
        var url = string.Format(TestConstants.ADMIN_USERS_USER_ID_PREMIUM_URL, user.Id);
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);
        TestConstants.AddIdempotencyKey(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Премиум и вправду установился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);
        Assert.True(userFromDbAfterUpdate.IsPremium);
    }

    [Fact]
    public async Task Put_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Запрос
        var url = string.Format(TestConstants.ADMIN_USERS_USER_ID_PREMIUM_URL, Guid.NewGuid());
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);
        TestConstants.AddIdempotencyKey(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.USER_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Put_ReturnsUserAlreadyHasPremium()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: true);

        // Запрос
        var url = string.Format(TestConstants.ADMIN_USERS_USER_ID_PREMIUM_URL, user.Id);
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);
        TestConstants.AddIdempotencyKey(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.USER_ALREADY_HAS_PREMIUM, jsonDocument.RootElement.GetProperty("code").GetString());
    }
}