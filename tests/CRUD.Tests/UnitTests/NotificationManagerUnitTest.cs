using CRUD.Models.Dtos.Notification;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace CRUD.Tests.UnitTests;

public sealed class NotificationManagerUnitTest
{
    private readonly ApplicationDbContext _db;

    private readonly Mock<IValidator<GetUserNotificationsDto>> _mockGetUserNotificationsDtoValidator;
    private readonly Mock<IValidator<CreateNotificationDto>> _mockCreateNotificationDtoValidator;
    private readonly Mock<IValidator<CreateNotificationSelectedUsersDto>> _mockCreateNotificationSelectedUsersDtoValidator;
    private readonly NotificationManager _notificationManager;

    public NotificationManagerUnitTest()
    {
        var db = DbContextGenerator.GenerateDbContextTestInMemory();
        _db = db;

        _mockGetUserNotificationsDtoValidator = new();
        _mockCreateNotificationDtoValidator = new();
        _mockCreateNotificationSelectedUsersDtoValidator = new();

        _notificationManager = new NotificationManager(
            db,
            _mockGetUserNotificationsDtoValidator.Object,
            _mockCreateNotificationDtoValidator.Object,
            _mockCreateNotificationSelectedUsersDtoValidator.Object
        );
    }

    [Fact]
    public async Task GetUserNotificationsDtoAsyncByCount_NotValidGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier()
    {
        // Arrange
        int count = 1;
        var userIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _notificationManager.GetUserNotificationsDtoAsync(userIdGuid, count);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }


    [Fact]
    public async Task CreateNotificationAsync_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        CreateNotificationDto createNotificationDto = null;

        // Act
        Func<Task> a = async () =>
        {
            await _notificationManager.CreateNotificationAsync(createNotificationDto);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(createNotificationDto), ex.ParamName);
    }


    [Fact]
    public async Task CreateNotificationAsyncbyCreateNotificationSelectedUsersDto_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        CreateNotificationSelectedUsersDto createNotificationSelectedUsersDto = null;

        // Act
        Func<Task> a = async () =>
        {
            await _notificationManager.CreateNotificationAsync(createNotificationSelectedUsersDto);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(createNotificationSelectedUsersDto), ex.ParamName);
    }


    [Fact]
    public async Task DeleteNotificationAsync_NotValidGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier()
    {
        // Arrange
        var notificationIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _notificationManager.DeleteNotificationAsync(notificationIdGuid);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }


    [Fact]
    public async Task SetIsReadNotificationAsync_NotValidGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier()
    {
        // Arrange
        var userIdGuid = Guid.Empty;
        var notificationIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _notificationManager.SetIsReadNotificationAsync(userIdGuid, notificationIdGuid);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }
}