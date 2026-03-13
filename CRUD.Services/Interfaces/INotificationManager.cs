using CRUD.Models.Dtos.Notification;

namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для работы с уведомлениями.
/// </summary>
public interface INotificationManager
{
    /// <summary>
    /// Получает указанное количество уведомлений указанного пользователя и преобразует в <see cref="UserNotificationDto"/>.
    /// </summary>
    /// <remarks>
    /// <para>Вызывающий метод должен предоставить валидные, не пустые данные для <paramref name="count"/>.</para>
    /// <para>Для валидации <paramref name="count"/> используется <see cref="IValidator{GetUserNotificationsDto}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="count"/> невалиден</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Пользователь не найден</term>
    /// <description><see cref="ErrorMessages.UserNotFound"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="count">Количество уведомлений.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/> или если <paramref name="count"/> невалиден.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="ServiceResult{IEnumerable{UserNotificationDto}}"/>, результат сервиса с <see cref="IEnumerable{UserNotificationDto}"/>, если уведомления найдены.</returns>
    Task<ServiceResult<IEnumerable<UserNotificationDto>>> GetUserNotificationsDtoAsync(Guid userId, int count, CancellationToken ct = default);

    /// <summary>
    /// Создаёт уведомление в базе по <see cref="CreateNotificationDto"/> и добавляет всем пользователям это уведомление.
    /// </summary>
    /// <remarks>
    /// <para>Вызывающий метод должен предоставить валидные, не пустые данные для <paramref name="createNotificationDto"/>.</para>
    /// <para>Для валидации <paramref name="createNotificationDto"/> используется <see cref="IValidator{CreateNotificationDto}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="createNotificationDto"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="createNotificationDto"/> невалиден</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если перед записью сущности <see cref="Notification"/> в базу, сущность окажется невалидна, изменения не последуют</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если возник конфликт параллельности</term>
    /// <description>исключение <see cref="DbUpdateConcurrencyException"/> | <see cref="DbUpdateException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="createNotificationDto">DTO-модель создания уведомления.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="createNotificationDto"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="createNotificationDto"/> невалиден или если после изменений данных сущности <see cref="Notification"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса с <see cref="NotificationDto"/>.</returns>
    Task<ServiceResult<NotificationDto>> CreateNotificationAsync(CreateNotificationDto createNotificationDto, CancellationToken ct = default);

    /// <summary>
    /// Создаёт уведомление в базе по <see cref="CreateNotificationSelectedUsersDto"/> и добавляет указанным пользователям из модели это уведомление.
    /// </summary>
    /// <remarks>
    /// <para>Вызывающий метод должен предоставить валидные, не пустые данные для <paramref name="createNotificationSelectedUsersDto"/>.</para>
    /// <para>Для валидации <paramref name="createNotificationSelectedUsersDto"/> используется <see cref="IValidator{CreateNotificationSelectedUsersDto}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="createNotificationSelectedUsersDto"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="createNotificationSelectedUsersDto"/> невалиден</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если перед записью сущности <see cref="Notification"/> в базу, сущность окажется невалидна, изменения не последуют</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если возник конфликт параллельности</term>
    /// <description>исключение <see cref="DbUpdateConcurrencyException"/> | <see cref="DbUpdateException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="createNotificationSelectedUsersDto">DTO-модель создания уведомления для указанных пользователей.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="createNotificationSelectedUsersDto"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="createNotificationSelectedUsersDto"/> невалиден или если после изменений данных сущности <see cref="Notification"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса с <see cref="NotificationDto"/>.</returns>
    Task<ServiceResult<NotificationDto>> CreateNotificationAsync(CreateNotificationSelectedUsersDto createNotificationSelectedUsersDto, CancellationToken ct = default);

    /// <summary>
    /// Удаляет уведомление полностью (даже у пользователей) из базы по указанному Id.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="notificationId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если возник конфликт параллельности</term>
    /// <description>исключение <see cref="DbUpdateConcurrencyException"/> | <see cref="DbUpdateException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Уведомление не найдено</term>
    /// <description><see cref="ErrorMessages.NotificationNotFound"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="notificationId">Id уведомления.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если <paramref name="notificationId"/> является <see cref="Guid.Empty"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса.</returns>
    Task<ServiceResult> DeleteNotificationAsync(Guid notificationId, CancellationToken ct = default);

    /// <summary>
    /// Задаёт указанному уведомлению указанному пользователю статус "прочитано".
    /// </summary>
    /// <remarks>
    /// <para>Для найденного <see cref="UserNotification"/> устанавливается <see cref="UserNotification.IsRead"/> <see langword="true"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="userId"/> или <paramref name="notificationId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если возник конфликт параллельности</term>
    /// <description>исключение <see cref="DbUpdateConcurrencyException"/> | <see cref="DbUpdateException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Уведомление пользователя не найдено</term>
    /// <description><see cref="ErrorMessages.UserNotificationNotFound"/>.</description>
    /// </item>
    /// <item>
    /// <term>Изменения не обнаружены</term>
    /// <description><see cref="ErrorMessages.NoChangesDetected"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="notificationId">Id уведомления.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> или <paramref name="notificationId"/> является <see cref="Guid.Empty"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса.</returns>
    Task<ServiceResult> SetIsReadNotificationAsync(Guid userId, Guid notificationId, CancellationToken ct = default);
}