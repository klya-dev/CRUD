using CRUD.Models.Domains;
using CRUD.Models.Dtos.Notification;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace CRUD.Tests.SystemTests.User;

public class UserNotificationsSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;

    public UserNotificationsSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
    }

    [Fact]
    public async Task Get_ReturnsIEnumerablePublicationDto()
    {
        // Arrange
        var client = _factory.HttpClient;
        int count = 2;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем уведомления в базу
        var notification = await DI.CreateNotificationAsync(_db);
        var notification2 = await DI.CreateNotificationAsync(_db);

        // Добавляем уведомления пользователей в базу
        var userNotification = await DI.CreateUserNotificationAsync(_db, user.Id, notification.Id);
        var userNotification2 = await DI.CreateUserNotificationAsync(_db, user.Id, notification2.Id);

        // Такой результат должен быть
        var mustResult = new List<UserNotificationDto>()
        {
            new UserNotificationDto
            {
                Id = notification.Id,
                Title = notification.Title,
                Content = notification.Content,
                CreatedAt = notification.CreatedAt.ToWithoutTicks(),
                IsRead = userNotification.IsRead
            },
            new UserNotificationDto
            {
                Id = notification2.Id,
                Title = notification2.Title,
                Content = notification2.Content,
                CreatedAt = notification2.CreatedAt.ToWithoutTicks(),
                IsRead = userNotification2.IsRead
            }
        }.Take(count);

        // Запрос
        var url = $"{TestConstants.USER_NOTIFICATIONS_URL}?count={count}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<IEnumerable<UserNotificationDto>>();

        Assert.Equivalent(mustResult, response);
    }

    [Fact]
    public async Task Get_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        int count = 1;
        var userIdGuid = Guid.NewGuid();

        // Запрос
        var url = $"{TestConstants.USER_NOTIFICATIONS_URL}?count={count}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request, _tokenManager, userId: userIdGuid.ToString());

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

    [Theory] // Корректные данные, но уведомлений нет вообще
    [InlineData(1)]
    [InlineData(25)]
    public async Task Get_WhenNotificationsNotExists_ReturnsEmptyCollection(int count)
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Запрос
        var url = $"{TestConstants.USER_NOTIFICATIONS_URL}?count={count}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<IEnumerable<UserNotificationDto>>();

        Assert.NotNull(response);
        Assert.Empty(response);
    }


    [Fact]
    public async Task Put_NotificationId_Read_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Добавляем уведомление в базу
        var notification = await DI.CreateNotificationAsync(_db);

        // Добавляем уведомление пользователя в базу
        var userNotification = await DI.CreateUserNotificationAsync(_db, user.Id, notification.Id);

        // Запрос
        var url = $"{string.Format(TestConstants.USER_NOTIFICATIONS_NOTIFICATIONS_ID_READ_URL, notification.Id)}";
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        TestConstants.AddBearerToken(request, _tokenManager, userId: userIdGuid.ToString());
        TestConstants.AddIdempotencyKey(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Уведомление и вправду обновилось
        var userNotificationFromDbAfterUpdate = await _db.UserNotifications.AsNoTracking().FirstAsync(x => x.UserId == userIdGuid && x.NotificationId == notification.Id);
        Assert.True(userNotificationFromDbAfterUpdate.IsRead);
    }

    [Fact]
    public async Task Put_NotificationId_Read_ReturnsUserNotificationNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        var userIdGuid = Guid.NewGuid();
        var notificationIdGuid = Guid.NewGuid();

        // Запрос
        var url = $"{string.Format(TestConstants.USER_NOTIFICATIONS_NOTIFICATIONS_ID_READ_URL, notificationIdGuid)}";
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        TestConstants.AddBearerToken(request, _tokenManager, userId: userIdGuid.ToString());
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

        Assert.Equal(ErrorCodes.USER_NOTIFICATION_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Put_NotificationId_Read_ReturnsNoChangesDetected()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем уведомление в базу
        var notification = await DI.CreateNotificationAsync(_db);

        // Добавляем уведомление пользователя в базу
        var userNotification = await DI.CreateUserNotificationAsync(_db, user.Id, notification.Id, isRead: true);

        // Запрос
        var url = $"{string.Format(TestConstants.USER_NOTIFICATIONS_NOTIFICATIONS_ID_READ_URL, notification.Id)}";
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());
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