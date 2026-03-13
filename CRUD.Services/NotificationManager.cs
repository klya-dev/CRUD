using CRUD.Models.Dtos.Notification;

namespace CRUD.Services;

/// <inheritdoc cref="INotificationManager"/>
public class NotificationManager : INotificationManager
{
    private readonly ApplicationDbContext _db;
    private readonly IValidator<Notification> _notificationValidator;
    private readonly IValidator<GetUserNotificationsDto> _getUserNotificationsDtoValidator;
    private readonly IValidator<CreateNotificationDto> _createNotificationDtoValidator;
    private readonly IValidator<CreateNotificationSelectedUsersDto> _createNotificationSelectedUsersDtoValidator;

    public NotificationManager(ApplicationDbContext db, IValidator<Notification> notificationValidator, IValidator<GetUserNotificationsDto> getUserNotificationsDtoValidator, IValidator<CreateNotificationDto> createNotificationDtoValidator, IValidator<CreateNotificationSelectedUsersDto> createNotificationSelectedUsersDtoValidator)
    {
        _db = db;
        _notificationValidator = notificationValidator;
        _getUserNotificationsDtoValidator = getUserNotificationsDtoValidator;
        _createNotificationDtoValidator = createNotificationDtoValidator;
        _createNotificationSelectedUsersDtoValidator = createNotificationSelectedUsersDtoValidator;
    }

    public async Task<ServiceResult<IEnumerable<UserNotificationDto>>> GetUserNotificationsDtoAsync(Guid userId, int count, CancellationToken ct = default)
    {
        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        var getUserNotificationsDto = new GetUserNotificationsDto()
        {
            Count = count
        };

        // Валидация модели
        var validationResult = await _getUserNotificationsDtoValidator.ValidateAsync(getUserNotificationsDto, ct);
        if (!validationResult.IsValid) // Эндпоинт должен предоставить валидные данные
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(GetUserNotificationsDto), validationResult.Errors));

        // Пользователь не найден
        var userExists = await _db.Users.AnyAsync(x => x.Id == userId, ct);
        if (!userExists)
            return ServiceResult<IEnumerable<UserNotificationDto>>.Fail(ErrorMessages.UserNotFound);

        // Достаём уведомления указанного пользователя и сразу преобразуем в DTO на стороне базы
        var notifications = await _db.UserNotifications.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Notification!.CreatedAt)
            .Take(count)
            .Select(x => x.ToUserNotificationDto(x.Notification!)) // EF сам подтянет зависимость
            .ToListAsync(ct);

        return ServiceResult<IEnumerable<UserNotificationDto>>.Success(notifications);
    }

    public async Task<ServiceResult<NotificationDto>> CreateNotificationAsync(CreateNotificationDto createNotificationDto, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(createNotificationDto);

        // Валидация модели
        var validationResult = await _createNotificationDtoValidator.ValidateAsync(createNotificationDto, ct);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(CreateNotificationDto), validationResult.Errors));

        var notification = new Notification
        {
            CreatedAt = DateTime.UtcNow,
            Title = createNotificationDto.Title,
            Content = createNotificationDto.Content
        };

        // Проверка валидности данных перед записью в базу
        var validationResultPublication = await _notificationValidator.ValidateAsync(notification, ct);
        if (!validationResultPublication.IsValid) // Если данные невалидны, то я уже ничего не сделаю - исключение
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(Notification), validationResultPublication.Errors));

        // Список кому и какое отправить уведомление
        IEnumerable<UserNotification> userNotifications = await _db.Users.Select(x => new UserNotification() { UserId = x.Id, NotificationId = notification.Id }).ToListAsync(ct);
        await _db.UserNotifications.AddRangeAsync(userNotifications, ct); // Добавляем уведомление всем пользователям

        await _db.Notifications.AddAsync(notification, ct);
        await _db.SaveChangesAsync(ct);

        return ServiceResult<NotificationDto>.Success(notification.ToNotificationDto());
    }

    public async Task<ServiceResult<NotificationDto>> CreateNotificationAsync(CreateNotificationSelectedUsersDto createNotificationSelectedUsersDto, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(createNotificationSelectedUsersDto);

        // Валидация модели
        var validationResult = await _createNotificationSelectedUsersDtoValidator.ValidateAsync(createNotificationSelectedUsersDto, ct);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(CreateNotificationSelectedUsersDto), validationResult.Errors));

        var notification = new Notification
        {
            CreatedAt = DateTime.UtcNow,
            Title = createNotificationSelectedUsersDto.Notification.Title,
            Content = createNotificationSelectedUsersDto.Notification.Content
        };

        // Проверка валидности данных перед записью в базу
        var validationResultPublication = await _notificationValidator.ValidateAsync(notification, ct);
        if (!validationResultPublication.IsValid) // Если данные невалидны, то я уже ничего не сделаю - исключение
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(Notification), validationResultPublication.Errors));

        // Список кому и какое отправить уведомление
        IEnumerable<UserNotification> userNotifications = [];
        if (createNotificationSelectedUsersDto.UserIds != null) // Если список пользователей не пустой
            userNotifications = await _db.Users.Where(x => createNotificationSelectedUsersDto.UserIds.Contains(x.Id)).Select(x => new UserNotification() { UserId = x.Id, NotificationId = notification.Id }).ToListAsync(ct); // Добавляем уведомление пользователям из списка

        await _db.UserNotifications.AddRangeAsync(userNotifications, ct);

        await _db.Notifications.AddAsync(notification, ct);
        await _db.SaveChangesAsync(ct);

        return ServiceResult<NotificationDto>.Success(notification.ToNotificationDto());
    }

    public async Task<ServiceResult> DeleteNotificationAsync(Guid notificationId, CancellationToken ct = default)
    {
        // Пустой GUID
        if (notificationId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Уведомление не найдено
        var notificationFromDb = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId, ct);
        if (notificationFromDb == null)
            return ServiceResult.Fail(ErrorMessages.NotificationNotFound);

        _db.Notifications.Remove(notificationFromDb);
        await _db.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetIsReadNotificationAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        // Пустой GUID
        if (userId == Guid.Empty || notificationId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Уведомление пользователя не найдено
        var userNotificationFromDb = await _db.UserNotifications.FirstOrDefaultAsync(x => x.UserId == userId && x.NotificationId == notificationId, ct);
        if (userNotificationFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotificationNotFound);

        // Не обнаружено изменений (уже прочитано)
        if (userNotificationFromDb.IsRead == true)
            return ServiceResult.Fail(ErrorMessages.NoChangesDetected);

        userNotificationFromDb.IsRead = true;

        // Валидатора нет, и пока что незачем

        _db.UserNotifications.Update(userNotificationFromDb);
        await _db.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}