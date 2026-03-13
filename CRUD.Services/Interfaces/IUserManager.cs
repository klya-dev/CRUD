using FluentValidation.Results;

namespace CRUD.Services.Interfaces;

/// <summary>
/// Основной сервис для работы с пользователями.
/// </summary>
public interface IUserManager
{
    /// <summary>
    /// Получает пользователя по предоставленному Id пользователя.
    /// </summary>
    /// <remarks>
    /// Флаг <paramref name="tracking"/>, позволяет указать, отслеживать ли изменения возвращённой сущности (EF, метод <see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}(IQueryable{TEntity})"/>).
    /// </remarks>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="tracking">Отслеживать ли изменения возвращённой сущности (EF, метод <see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}(IQueryable{TEntity})"/>).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="User"/>, если пользователь не найден, то возвращается <see langword="null"/>.</returns>
    Task<User?> GetUserAsync(Guid userId, bool tracking = true, CancellationToken ct = default);

    /// <summary>
    /// Получает пользователя по предоставленному Username'у пользователя.
    /// </summary>
    /// <remarks>
    /// Флаг <paramref name="tracking"/>, позволяет указать, отслеживать ли изменения возвращённой сущности (EF, метод <see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}(IQueryable{TEntity})"/>).
    /// </remarks>
    /// <param name="username">Username пользователя.</param>
    /// <param name="tracking">Отслеживать ли изменения возвращённой сущности (EF, метод <see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}(IQueryable{TEntity})"/>).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="User"/>, если пользователь не найден, то возвращается <see langword="null"/>.</returns>
    Task<User?> GetUserAsync(string username, bool tracking = true, CancellationToken ct = default);

    /// <summary>
    /// Получает DTO-модель пользователя по предоставленному Id пользователя.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
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
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="ServiceResult{UserDto}"/>, результат сервиса с <see cref="UserDto"/>, если пользователь найден.</returns>
    Task<ServiceResult<UserDto>> GetUserDtoAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Получает DTO-модель полных данных пользователя по предоставленному Id пользователя.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
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
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="ServiceResult{UserFullDto}"/>, результат сервиса с <see cref="UserFullDto"/>, если пользователь найден.</returns>
    Task<ServiceResult<UserFullDto>> GetUserFullDtoAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Полностью обновляет любые данные пользователя по предоставленной валидной модели <see cref="User"/>.
    /// </summary>
    /// <remarks>
    /// <para>Вызывающий метод должен предоставить валидные, не пустые данные для <paramref name="changedUser"/>.</para>
    /// <para>Для валидации <paramref name="changedUser"/> используется <see cref="IValidator{User}"/>.</para>
    /// <para>Нет ни одной бизнес проверки!</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="changedUser"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="changedUser"/> невалидна</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если возник конфликт параллельности или другая проблема обновления</term>
    /// <description>исключение <see cref="DbUpdateConcurrencyException"/> | <see cref="DbUpdateException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="changedUser">Изменённая модель пользователя для обновления.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="changedUser"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="changedUser"/> невалиден.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности или другая проблема обновления.</exception>
    Task UpdateUserAsync(User changedUser, CancellationToken ct = default);

    /// <summary>
    /// Обновляет данные пользователя по предоставленной валидной модели <see cref="UpdateUserDto"/>.
    /// </summary>
    /// <remarks>
    /// <para>Вызывающий метод должен предоставить валидные, не пустые данные для <paramref name="updateUserDto"/>.</para>
    /// <para>Для валидации <paramref name="updateUserDto"/> используется <see cref="IValidator{UpdateUserDto}"/>.</para>
    /// <para>Для валидации <see cref="User"/> используется <see cref="IValidator{User}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="updateUserDto"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="updateUserDto"/> невалидна</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если после изменений данных сущности <see cref="User"/>, сущность окажется невалидна, изменения не последуют</term>
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
    /// <term>Пользователь не найден</term>
    /// <description><see cref="ErrorMessages.UserNotFound"/>.</description>
    /// </item>
    /// <item>
    /// <term>Не обнаружено изменений</term>
    /// <description><see cref="ErrorMessages.NoChangesDetected"/>.</description>
    /// </item>
    /// <item>
    /// <term>Username уже занят</term>
    /// <description><see cref="ErrorMessages.UsernameAlreadyTaken"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="updateUserDto">DTO-модель для обновления данных пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="updateUserDto"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/> или если <paramref name="updateUserDto"/> невалиден или если после изменений данных сущности <see cref="User"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса.</returns>
    Task<ServiceResult> UpdateUserAsync(Guid userId, UpdateUserDto updateUserDto, CancellationToken ct = default);

    /// <summary>
    /// Удаляет пользователя из базы данных по предоставленной валидной модели <see cref="DeleteUserDto"/>.
    /// </summary>
    /// <remarks>
    /// <para>Аватарка пользователя тоже удаляется.</para>
    /// <para>Вызывающий метод должен предоставить валидные, не пустые данные для <paramref name="deleteUserDto"/>.</para>
    /// <para>Для валидации <paramref name="deleteUserDto"/> используется <see cref="IValidator{DeleteUserDto}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="deleteUserDto"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="deleteUserDto"/> невалидна</term>
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
    /// <term>Пользователь не найден</term>
    /// <description><see cref="ErrorMessages.UserNotFound"/>.</description>
    /// </item>
    /// <item>
    /// <term>Неверный пароль</term>
    /// <description><see cref="ErrorMessages.InvalidPassword"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="deleteUserDto">DTO-модель для удаления пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="deleteUserDto"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/> или если <paramref name="deleteUserDto"/> невалиден.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса.</returns>
    Task<ServiceResult> DeleteUserAsync(Guid userId, DeleteUserDto deleteUserDto, CancellationToken ct = default);

    /// <summary>
    /// Удаляет пользователя из базы данных.
    /// </summary>
    /// <remarks>
    /// <para>Аватарка пользователя тоже удаляется.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
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
    /// <term>Пользователь не найден</term>
    /// <description><see cref="ErrorMessages.UserNotFound"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса.</returns>
    Task<ServiceResult> DeleteUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Создаёт пользователя в базе данных по предоставленной валидной модели <see cref="CreateUserDto"/>.
    /// </summary>
    /// <remarks>
    /// <para>Вызывающий метод должен предоставить валидные, не пустые данные для <paramref name="createUserDto"/>.</para>
    /// <para>Для валидации <paramref name="createUserDto"/> используется <see cref="IValidator{CreateUserDto}"/>.</para>
    /// <para>Для валидации <see cref="User"/> используется <see cref="IValidator{User}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="createUserDto"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="createUserDto"/> невалидна</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если после изменений данных сущности <see cref="User"/>, сущность окажется невалидна, изменения не последуют</term>
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
    /// <term>Username уже занят</term>
    /// <description><see cref="ErrorMessages.UsernameAlreadyTaken"/>.</description>
    /// </item>
    /// <item>
    /// <term>Email уже занят</term>
    /// <description><see cref="ErrorMessages.EmailAlreadyTaken"/>.</description>
    /// </item>
    /// <item>
    /// <term>Номер телефона уже занят</term>
    /// <description><see cref="ErrorMessages.PhoneNumberAlreadyTaken"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="createUserDto">DTO-модель для создания пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="createUserDto"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="createUserDto"/> невалидна или если после изменений данных сущности <see cref="User"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса с <see cref="User"/>.</returns>
    Task<ServiceResult<User>> CreateUserAsync(CreateUserDto createUserDto, CancellationToken ct = default);

    /// <summary>
    /// Создаёт пользователя в базе данных по <see cref="OpenIdUserInfo"/> и <see cref="OAuthCompleteRegistrationDto"/>.
    /// </summary>
    /// <remarks>
    /// <para>Вызывающий метод должен предоставить валидные, не пустые данные для <paramref name="oAuthCompleteRegistrationDto"/>.</para>
    /// <para>Для валидации <paramref name="oAuthCompleteRegistrationDto"/> используется <see cref="IValidator{OAuthCompleteRegistrationDto}"/>.</para>
    /// <para>Для валидации <see cref="CreateUserDto"/> используется <see cref="IValidator{CreateUserDto}"/>.</para>
    /// <para>Для валидации <see cref="User"/> используется <see cref="IValidator{User}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="userInfo"/> или <paramref name="oAuthCompleteRegistrationDto"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="oAuthCompleteRegistrationDto"/> или <see cref="CreateUserDto"/> невалидна</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если после изменений данных сущности <see cref="User"/>, сущность окажется невалидна, изменения не последуют</term>
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
    /// <term>Email уже занят</term>
    /// <description><see cref="ErrorMessages.EmailAlreadyTaken"/>.</description>
    /// </item>
    /// <item>
    /// <term>Номер телефона уже занят</term>
    /// <description><see cref="ErrorMessages.PhoneNumberAlreadyTaken"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userInfo">Информация о пользователе OpenId.</param>
    /// <param name="oAuthCompleteRegistrationDto">DTO-модель завершения регистрации через OAuth.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="userInfo"/> или <paramref name="oAuthCompleteRegistrationDto"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="oAuthCompleteRegistrationDto"/> или <see cref="CreateUserDto"/> невалидна или если после изменений данных сущности <see cref="User"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса с <see cref="User"/>.</returns>
    Task<ServiceResult<User>> CreateUserAsync(OpenIdUserInfo userInfo, OAuthCompleteRegistrationDto oAuthCompleteRegistrationDto, CancellationToken ct = default);

    /// <summary>
    /// Устанавливает роль пользователю по указанной модели.
    /// </summary>
    /// <remarks>
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="setRoleDto"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="setRoleDto"/> невалидна</term>
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
    /// <term>Пользователь не найден</term>
    /// <description><see cref="ErrorMessages.UserNotFound"/>.</description>
    /// </item>
    /// <item>
    /// <term>Не обнаружено изменений</term>
    /// <description><see cref="ErrorMessages.NoChangesDetected"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="setRoleDto">DTO-модель устанавливаемой роль.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="setRoleDto"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/> или если <paramref name="setRoleDto"/> невалиден.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса.</returns>
    Task<ServiceResult> SetRoleUserAsync(Guid userId, SetRoleDto setRoleDto, CancellationToken ct = default);

    /// <summary>
    /// Удаляет все Refresh-токены пользователя.
    /// </summary>
    /// <remarks>
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
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
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса.</returns>
    Task<ServiceResult> RevokeRefreshTokensAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Проверяет занят ли <see cref="User.Username"/> в базе данных по предоставленному Username'у.
    /// </summary>
    /// <param name="username">Username пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see langword="true"/>, если занят.</returns>
    Task<bool> IsUsernameAlreadyTakenAsync(string username, CancellationToken ct = default);

    /// <summary>
    /// Проверяет занят ли <see cref="User.Email"/> в базе данных по предоставленному Email'у.
    /// </summary>
    /// <param name="email">Электронная почта пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see langword="true"/>, если занят.</returns>
    Task<bool> IsEmailAlreadyTakenAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Проверяет занят ли <see cref="User.PhoneNumber"/> в базе данных по предоставленному PhoneNumber'у.
    /// </summary>
    /// <param name="phoneNumber">Телефонный номер пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see langword="true"/>, если занят.</returns>
    Task<bool> IsPhoneNumberAlreadyTakenAsync(string phoneNumber, CancellationToken ct = default);

    /// <summary>
    /// Проверяет существует ли пользователь в базе данных по предоставленному Username'у.
    /// </summary>
    /// <param name="username">Username пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see langword="true"/>, если существует.</returns>
    Task<bool> IsUserExistsAsync(string username, CancellationToken ct = default);

    /// <summary>
    /// Проверяет существует ли пользователь в базе данных по предоставленному Id пользователя.
    /// </summary>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><see langword="true"/>, если существует.</returns>
    Task<bool> IsUserExistsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Проверяет валидность предоставленной модели пользователя.
    /// </summary>
    /// <remarks>
    /// Для валидации используется <see cref="IValidator{User}"/>.
    /// </remarks>
    /// <param name="user">Модель пользователя.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="ValidationResult"/>, результат валидации.</returns>
    ValidationResult Validate(User user);

    /// <summary>
    /// Асинхронно проверяет валидность предоставленной модели пользователя.
    /// </summary>
    /// <remarks>
    /// Для валидации используется <see cref="IValidator{User}"/>.
    /// </remarks>
    /// <param name="user">Модель пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="ValidationResult"/>, результат валидации.</returns>
    Task<ValidationResult> ValidateAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Подтверждает электронную почту пользователя по предоставленному токену.
    /// </summary>
    /// <remarks>
    /// <para>Для валидации <see cref="User"/> используется <see cref="IValidator{User}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="token"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если после изменений данных сущности <see cref="User"/>, сущность окажется невалидна, изменения не последуют</term>
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
    /// <term>Неверный токен</term>
    /// <description><see cref="ErrorMessages.InvalidToken"/>.</description>
    /// </item>
    /// <item>
    /// <term>Пользователь не найден</term>
    /// <description><see cref="ErrorMessages.UserNotFound"/>.</description>
    /// </item>
    /// <item>
    /// <term>Электронная почта пользователя уже подтверждёна</term>
    /// <description><see cref="ErrorMessages.UserAlreadyConfirmedEmail"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="token">Токен для подтверждения электронной почты.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="token"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если после изменений данных сущности <see cref="User"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса.</returns>
    Task<ServiceResult> ConfirmEmailAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Подтверждает телефонный номер пользователя по предоставленному Id пользователя и коду.
    /// </summary>
    /// <remarks>
    /// <para>Для валидации <see cref="User"/> используется <see cref="IValidator{User}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="token"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если после изменений данных сущности <see cref="User"/>, сущность окажется невалидна, изменения не последуют</term>
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
    /// <term>Неверный код</term>
    /// <description><see cref="ErrorMessages.InvalidCode"/>.</description>
    /// </item>
    /// <item>
    /// <term>Пользователь не найден</term>
    /// <description><see cref="ErrorMessages.UserNotFound"/>.</description>
    /// </item>
    /// <item>
    /// <term>Телефонный номер пользователя уже подтверждён</term>
    /// <description><see cref="ErrorMessages.UserAlreadyConfirmedPhoneNumber"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="code">Код для подтверждения телефонного номера.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="token"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/> или если после изменений данных сущности <see cref="User"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса.</returns>
    Task<ServiceResult> VerificatePhoneNumberAsync(Guid userId, string code, CancellationToken ct = default);

    /// <summary>
    /// Создаёт Админ-пользователя в базе данных.
    /// </summary>
    /// <remarks>
    /// Username: admin, Password: 123.
    /// </remarks>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    Task CreateAdminUserAsync(CancellationToken ct = default);
}