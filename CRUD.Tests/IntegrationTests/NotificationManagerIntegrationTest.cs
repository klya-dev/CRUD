using CRUD.Models.Domains;
using CRUD.Models.Dtos.Notification;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace CRUD.Tests.IntegrationTests;

public class NotificationManagerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly INotificationManager _notificationManager;
    private readonly ApplicationDbContext _db;

    public NotificationManagerIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _notificationManager = scopedServices.GetRequiredService<INotificationManager>();
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
    }

    [Theory] // Корректные данные
    [InlineData(1)]
    [InlineData(25)]
    public async Task GetUserNotificationsDtoAsyncByCount_ReturnsIEnumerableUserNotificationDto(int count)
    {
        // Arrange
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

        // Act
        var result = await _notificationManager.GetUserNotificationsDtoAsync(user.Id, count);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value);

        Assert.Equivalent(mustResult, result.Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(101)]
    public async Task GetUserNotificationsDtoAsyncByCount_NotValidData_ThrowsInvalidOperationException(int count)
    {
        // Arrange
        var getUserNotificationsDto = new GetUserNotificationsDto()
        {
            Count = count
        };
        var validatorsLocalizer = new Models.Validators.ValidatorsLocalizer.ValidatorsLocalizer();
        var validationResult = await new GetUserNotificationsDtoValidator(validatorsLocalizer).ValidateAsync(getUserNotificationsDto);

        // Act
        Func<Task> a = async () =>
        {
            await _notificationManager.GetUserNotificationsDtoAsync(Guid.NewGuid(), count);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(GetUserNotificationsDto), validationResult.Errors), ex.Message);
    }

    [Fact]
    public async Task GetUserNotificationsDtoAsyncByCount_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        int count = 1;
        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _notificationManager.GetUserNotificationsDtoAsync(userIdGuid, count);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);

        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }

    [Theory] // Корректные данные, но уведомлений нет вообще
    [InlineData(1)]
    [InlineData(25)]
    public async Task GetUserNotificationsDtoAsyncByCount_WhenNotificationsNotExists_ReturnsEmptyCollection(int count)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Act
        var result = await _notificationManager.GetUserNotificationsDtoAsync(user.Id, count);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }


    [Fact] // Корректные данные
    public async Task CreateNotificationAsync_ReturnsServiceResult()
    {
        // Arrange
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

        // Act
        var result = await _notificationManager.CreateNotificationAsync(createNotificationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Уведомление и вправду создалось | нахожу по DTO
        var notificationFromDbAfterCreate = await _db.Notifications.AsNoTracking().FirstAsync(x => x.Title == title && x.Content == content);
        Assert.Equal(createNotificationDto.Title, notificationFromDbAfterCreate.Title);
        Assert.Equal(createNotificationDto.Content, notificationFromDbAfterCreate.Content);

        // Уведомление пользователя и вправду создалось
        var userNotificationFromDbAfterCreate = await _db.UserNotifications.AsNoTracking().FirstAsync(x => x.NotificationId == notificationFromDbAfterCreate.Id);
        Assert.Equal(userNotificationFromDbAfterCreate.UserId, userIdGuid);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(null, null)] // Пустые данные
    public async Task CreateNotificationAsync_NotValidData_ThrowsInvalidOperationException(string title, string content)
    {
        // Arrange
        var createNotificationDto = new CreateNotificationDto()
        {
            Title = title,
            Content = content
        };
        var userIdGuid = Guid.NewGuid();
        var validatorsLocalizer = new Models.Validators.ValidatorsLocalizer.ValidatorsLocalizer();
        var validationResult = await new CreateNotificationDtoValidator(validatorsLocalizer).ValidateAsync(createNotificationDto);

        // Act
        Func<Task> a = async () =>
        {
            await _notificationManager.CreateNotificationAsync(createNotificationDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(CreateNotificationDto), validationResult.Errors), ex.Message);
    }


    [Fact] // Корректные данные
    public async Task CreateNotificationAsyncByCreateNotificationSelectedUsersDto_ReturnsServiceResult()
    {
        // Arrange
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

        // Act
        var result = await _notificationManager.CreateNotificationAsync(createNotificationSelectedUsersDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Уведомление и вправду создалось | нахожу по DTO
        var notificationFromDbAfterCreate = await _db.Notifications.AsNoTracking().FirstAsync(x => x.Title == title && x.Content == content);
        Assert.Equal(createNotificationSelectedUsersDto.Notification.Title, notificationFromDbAfterCreate.Title);
        Assert.Equal(createNotificationSelectedUsersDto.Notification.Content, notificationFromDbAfterCreate.Content);

        // Уведомление для первого пользователя и вправду создалось
        var userNotificationFromDbAfterCreate = await _db.UserNotifications.AsNoTracking().FirstAsync(x => x.NotificationId == notificationFromDbAfterCreate.Id);
        Assert.Equal(userNotificationFromDbAfterCreate.UserId, userIdGuid);

        // Уведомление для второго пользователя и вправду не создалось (т.к его нет в списке)
        var user2NotificationFromDbAfterCreate = await _db.UserNotifications.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == user2.Id);
        Assert.Null(user2NotificationFromDbAfterCreate);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(null, null)] // Пустые данные
    public async Task CreateNotificationAsyncByCreateNotificationSelectedUsersDto_NotValidData_ThrowsInvalidOperationException(string title, string content)
    {
        // Arrange
        var createNotificationSelectedUsersDto = new CreateNotificationSelectedUsersDto()
        {
            UserIds = [],
            Notification = new CreateNotificationDto()
            {
                Title = title,
                Content = content
            }
        };
        var userIdGuid = Guid.NewGuid();
        var validatorsLocalizer = new Models.Validators.ValidatorsLocalizer.ValidatorsLocalizer();
        var validationResult = await new CreateNotificationSelectedUsersDtoValidator(validatorsLocalizer).ValidateAsync(createNotificationSelectedUsersDto);

        // Act
        Func<Task> a = async () =>
        {
            await _notificationManager.CreateNotificationAsync(createNotificationSelectedUsersDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(CreateNotificationSelectedUsersDto), validationResult.Errors), ex.Message);
    }


    [Fact] // Корректные данные
    public async Task DeleteNotificationAsync_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем уведомление в базу
        var notification = await DI.CreateNotificationAsync(_db);

        // Добавляем уведомление пользователя в базу
        var userNotification = await DI.CreateUserNotificationAsync(_db, user.Id, notification.Id);

        // Act
        var result = await _notificationManager.DeleteNotificationAsync(notification.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Уведомление и вправду удалилась
        var notificationFromDbAfterDelete = await _db.Notifications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == notification.Id);
        Assert.Null(notificationFromDbAfterDelete);
    }

    [Fact]
    public async Task DeleteNotificationAsync_ReturnsErrorMessage_NotificationNotFound()
    {
        // Arrange
        var notificationIdGuid = Guid.NewGuid();

        // Act
        var result = await _notificationManager.DeleteNotificationAsync(notificationIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.NotificationNotFound, result.ErrorMessage);
    }


    [Fact]
    public async Task SetIsReadNotificationAsync_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Добавляем уведомление в базу
        var notification = await DI.CreateNotificationAsync(_db);

        // Добавляем уведомление пользователя в базу
        var userNotification = await DI.CreateUserNotificationAsync(_db, user.Id, notification.Id);

        // Act
        var result = await _notificationManager.SetIsReadNotificationAsync(userIdGuid, notification.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Уведомление и вправду обновилось
        var userNotificationFromDbAfterUpdate = await _db.UserNotifications.AsNoTracking().FirstAsync(x => x.UserId == userIdGuid && x.NotificationId == notification.Id);
        Assert.True(userNotificationFromDbAfterUpdate.IsRead);
    }

    [Fact]
    public async Task SetIsReadNotificationAsync_ReturnsErrorMessage_UserNotificationNotFound()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();
        var notificationIdGuid = Guid.NewGuid();

        // Act
        var result = await _notificationManager.SetIsReadNotificationAsync(userIdGuid, notificationIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotificationNotFound, result.ErrorMessage);
    }

    [Fact]
    public async Task SetIsReadNotificationAsync_ReturnsErrorMessage_NoChangesDetected()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем уведомление в базу
        var notification = await DI.CreateNotificationAsync(_db);

        // Добавляем уведомление пользователя в базу
        var userNotification = await DI.CreateUserNotificationAsync(_db, user.Id, notification.Id, isRead: true);

        // Act
        var result = await _notificationManager.SetIsReadNotificationAsync(user.Id, notification.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.NoChangesDetected, result.ErrorMessage);
    }


    // Если удалить пользователя или уведомление, то UserNotification, связывающий их, тоже удалится
    [Fact]
    public async Task DeleteUser_WhenUserHaveNotifications_UserNotificationDeleted()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем уведомления в базу
        var notification = await DI.CreateNotificationAsync(_db);
        var notification2 = await DI.CreateNotificationAsync(_db);

        // Добавляем уведомления пользователя в базу
        var userNotification = await DI.CreateUserNotificationAsync(_db, user.Id, notification.Id);
        var userNotification2 = await DI.CreateUserNotificationAsync(_db, user.Id, notification2.Id);

        var userNotificationsFromDbBeforeDelete = await _db.UserNotifications.AsNoTracking().Where(x => x.UserId == user.Id).ToListAsync();

        // Act
        // Удаляем пользователя
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        // Assert
        Assert.Equal(2, userNotificationsFromDbBeforeDelete.Count);

        // Все UserNotification и вправду удалились
        var userNotificationsFromDbAfterDelete = await _db.UserNotifications.AsNoTracking().Where(x => x.UserId == user.Id).ToListAsync();
        Assert.Empty(userNotificationsFromDbAfterDelete);
    }

    [Fact]
    public async Task DeleteNotification_WhenUserHaveNotifications_UserNotificationDeleted()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем уведомления в базу
        var notification = await DI.CreateNotificationAsync(_db);
        var notification2 = await DI.CreateNotificationAsync(_db);

        // Добавляем уведомления пользователя в базу
        var userNotification = await DI.CreateUserNotificationAsync(_db, user.Id, notification.Id);
        var userNotification2 = await DI.CreateUserNotificationAsync(_db, user.Id, notification2.Id);

        var userNotificationsFromDbBeforeDelete = await _db.UserNotifications.AsNoTracking().Where(x => x.UserId == user.Id).ToListAsync();

        // Act
        // Удаляем первое уведомление
        _db.Notifications.Remove(notification);
        await _db.SaveChangesAsync();

        // Assert
        Assert.Equal(2, userNotificationsFromDbBeforeDelete.Count);

        // Первый UserNotification и вправду удалился
        var userNotificationsFromDbAfterDelete = await _db.UserNotifications.AsNoTracking().Where(x => x.UserId == user.Id).ToListAsync();
        Assert.Single(userNotificationsFromDbAfterDelete); // В базе осталось только одно уведомление пользователя
        Assert.Equal(notification2.Id, userNotificationsFromDbAfterDelete.Single().NotificationId);
    }
}