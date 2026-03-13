using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CRUD.Tests.SystemTests.Admin;

public class AdminUsersRoleSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;

    public AdminUsersRoleSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
    }

    [Theory]
    [InlineData(UserRoles.Admin, UserRoles.User)]
    [InlineData(UserRoles.User, UserRoles.Admin)]
    public async Task Put_ReturnsNoContent(string currentRole, string role)
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, role: currentRole);

        // Данные
        var data = new SetRoleDto()
        {
            Role = role
        };

        // Запрос
        var url = string.Format(TestConstants.ADMIN_USERS_USER_ID_ROLE_URL, user.Id);
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);
        TestConstants.AddIdempotencyKey(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Роль и вправду обновилась
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);
        Assert.Equal(role, userFromDbAfterUpdate.Role);
    }

    [Fact]
    public async Task Put_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Данные
        var data = new SetRoleDto()
        {
            Role = UserRoles.Admin
        };

        // Запрос
        var url = string.Format(TestConstants.ADMIN_USERS_USER_ID_ROLE_URL, Guid.NewGuid());
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
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
    public async Task Put_ReturnsNoChangesDetected()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, role: UserRoles.Admin);

        // Данные
        var data = new SetRoleDto()
        {
            Role = UserRoles.Admin
        };

        // Запрос
        var url = string.Format(TestConstants.ADMIN_USERS_USER_ID_ROLE_URL, user.Id);
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
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

        Assert.Equal(ErrorCodes.NO_CHANGES_DETECTED, jsonDocument.RootElement.GetProperty("code").GetString());
    }
}