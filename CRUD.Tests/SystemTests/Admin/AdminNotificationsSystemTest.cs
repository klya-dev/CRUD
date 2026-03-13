using CRUD.Models.Dtos.Notification;
using CRUD.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CRUD.Tests.SystemTests.Admin;

public class AdminNotificationsSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;

    public AdminNotificationsSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
    }

    [Fact]
    public async Task Get_Users_UserId_ReturnsIEnumerablePublicationDto()
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
        var url = $"{string.Format(TestConstants.ADMIN_NOTIFICATIONS_USERS_USER_ID_URL, user.Id)}?count={count}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

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
    public async Task Get_Users_UserId_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        int count = 1;
        var userIdGuid = Guid.NewGuid();

        // Запрос
        var url = $"{string.Format(TestConstants.ADMIN_NOTIFICATIONS_USERS_USER_ID_URL, userIdGuid)}?count={count}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

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
    public async Task Get_Users_UserId_WhenNotificationsNotExists_ReturnsEmptyCollection(int count)
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Запрос
        var url = $"{string.Format(TestConstants.ADMIN_NOTIFICATIONS_USERS_USER_ID_URL, user.Id)}?count={count}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

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


    [Fact] // Корректные данные
    public async Task Post_ShouldCreateAndSend_ReturnsCreated()
    {
        // Arrange
        var client = _factory.HttpClient;
        var server = _factory.Server;

        string title = "title";
        string content = "content";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        var createNotificationDto = new CreateNotificationDto()
        {
            Title = title,
            Content = content
        };

        // Запрос
        var url = $"{TestConstants.ADMIN_NOTIFICATIONS_URL}";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        var json = new StringContent(JsonSerializer.Serialize(createNotificationDto), Encoding.UTF8, Application.Json);
        request.Content = json;
        var token = TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

        // Подключаемся к хабу
        var hubConnection = new HubConnectionBuilder()
            .WithUrl($"http://localhost/{TestConstants.NOTIFICATION_HUB_URL}", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token)!; // Аутентификация
                options.HttpMessageHandlerFactory = _ => server.CreateHandler(); // Чтобы подключится
            })
            .AddMessagePackProtocol()
            .Build();

        // Получаем уведомление, которое пришло клиенту с хаба
        NotificationDto notificationFromClient = null;
        hubConnection.On<NotificationDto>(HubMethodNames.ReceiveNotification, (notification) =>
        {
            notificationFromClient = notification;
        });
        await hubConnection.StartAsync();

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Created, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<NotificationDto>();

        Assert.NotNull(response);

        // Уведомление и вправду создалось
        var notificationFromDbAfterCreate = await _db.Notifications.AsNoTracking().FirstAsync(x => x.Id == response.Id);
        Assert.Equal(createNotificationDto.Title, notificationFromDbAfterCreate.Title);
        Assert.Equal(createNotificationDto.Content, notificationFromDbAfterCreate.Content);

        // Уведомление пользователя и вправду создалось
        var userNotificationFromDbAfterCreate = await _db.UserNotifications.AsNoTracking().FirstAsync(x => x.NotificationId == notificationFromDbAfterCreate.Id);
        Assert.Equal(userNotificationFromDbAfterCreate.UserId, userIdGuid);

        // Проверяем то, что пришло клиенту с хаба
        Assert.Equal(notificationFromDbAfterCreate.Id, notificationFromClient.Id);
    }


    [Fact] // Корректные данные
    public async Task Post_ShouldCreateAndSend_SelectedUsers_ReturnsCreated()
    {
        // Arrange
        var client = _factory.HttpClient;
        var server = _factory.Server;

        string title = "title";
        string content = "content";

        // Добавляем пользователей в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;
        var user2 = await DI.CreateUserAsync(_db, username: "test", email: "test@test.ru", phoneNumber: "123456789");

        var createNotificationSelectedUsersDto = new CreateNotificationSelectedUsersDto()
        {
            UserIds = [userIdGuid], // Создаём уведомление только для первого пользователя
            Notification = new CreateNotificationDto()
            {
                Title = title,
                Content = content
            }
        };

        // Запрос
        var url = $"{TestConstants.ADMIN_NOTIFICATIONS_SELECTED_USERS_URL}";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        var json = new StringContent(JsonSerializer.Serialize(createNotificationSelectedUsersDto), Encoding.UTF8, Application.Json);
        request.Content = json;
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

        // Токен первого пользователя (ему придёт уведомление)
        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userIdGuid.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("language_code", user.LanguageCode),
            new Claim("premium", user.IsPremium.ToString())
        ];
        var token = _tokenManager.GenerateAuthResponse(claims, user.Username).AccessToken;

        // Подключаемся к хабу
        var hubConnection = new HubConnectionBuilder()
            .WithUrl($"http://localhost/{TestConstants.NOTIFICATION_HUB_URL}", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token)!; // Токен первого пользователя
                options.HttpMessageHandlerFactory = _ => server.CreateHandler(); // Чтобы подключится
            })
            .AddMessagePackProtocol()
            .Build();

        // Получаем уведомление, которое пришло клиенту с хаба
        NotificationDto notificationFromClient = null;
        hubConnection.On<NotificationDto>(HubMethodNames.ReceiveNotification, (notification) =>
        {
            notificationFromClient = notification;
        });
        await hubConnection.StartAsync();

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Created, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);
        var response = jsonDocument.Deserialize<NotificationDto>();

        Assert.NotNull(response);

        // Уведомление и вправду создалось | нахожу по DTO
        var notificationFromDbAfterCreate = await _db.Notifications.AsNoTracking().FirstAsync(x => x.Id == response.Id);
        Assert.Equal(createNotificationSelectedUsersDto.Notification.Title, notificationFromDbAfterCreate.Title);
        Assert.Equal(createNotificationSelectedUsersDto.Notification.Content, notificationFromDbAfterCreate.Content);

        // Уведомление для первого пользователя и вправду создалось
        var userNotificationFromDbAfterCreate = await _db.UserNotifications.AsNoTracking().FirstAsync(x => x.NotificationId == notificationFromDbAfterCreate.Id);
        Assert.Equal(userNotificationFromDbAfterCreate.UserId, userIdGuid);

        // Уведомление для второго пользователя и вправду не создалось (т.к его нет в списке)
        var user2NotificationFromDbAfterCreate = await _db.UserNotifications.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == user2.Id);
        Assert.Null(user2NotificationFromDbAfterCreate);

        // Проверяем то, что пришло клиенту с хаба
        Assert.Equal(notificationFromDbAfterCreate.Id, notificationFromClient.Id);
    }


    [Fact] // Корректные данные
    public async Task Delete_NotificationId_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем уведомление в базу
        var notification = await DI.CreateNotificationAsync(_db);

        // Добавляем уведомление пользователя в базу
        var userNotification = await DI.CreateUserNotificationAsync(_db, user.Id, notification.Id);

        // Запрос
        var url = $"{string.Format(TestConstants.ADMIN_NOTIFICATIONS_NOTIFICATIONS_ID_URL, notification.Id)}";
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);
        TestConstants.AddIdempotencyKey(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Уведомление и вправду удалилась
        var notificationFromDbAfterDelete = await _db.Notifications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == notification.Id);
        Assert.Null(notificationFromDbAfterDelete);
    }

    [Fact]
    public async Task Delete_NotificationId_ReturnsNotificationNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        var notificationIdGuid = Guid.NewGuid();

        // Запрос
        var url = $"{string.Format(TestConstants.ADMIN_NOTIFICATIONS_NOTIFICATIONS_ID_URL, notificationIdGuid)}";
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
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

        Assert.Equal(ErrorCodes.NOTIFICATION_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }
}