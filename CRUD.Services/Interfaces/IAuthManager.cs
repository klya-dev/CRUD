using CRUD.Models.Dtos;

namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для аутентификации и авторизации.
/// </summary>
public interface IAuthManager
{
    /// <summary>
    /// Аутентифицирует пользователя в системе и возвращает JWT-токен для аутентификации.
    /// </summary>
    /// <remarks>
    /// <para>Вызывающий метод должен предоставить валидные, не пустые данные для <paramref name="loginData"/>.</para>
    /// <para>Для валидации <paramref name="loginData"/> используется <see cref="IValidator{LoginData}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="loginData"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="loginData"/> невалидна</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Пользователь не найден | Неверный логин или пароль</term>
    /// <description><see cref="ErrorMessages.InvalidLoginOrPassword"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="loginData">DTO-модель для аутентификации пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="loginData"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="loginData"/> невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns>Результат сервиса <see cref="ServiceResult{AuthJwtResponse}"/> с токенами аутентификации.</returns>
    Task<ServiceResult<AuthJwtResponse>> LoginAsync(LoginDataDto loginData, CancellationToken ct = default);

    /// <summary>
    /// Аутентифицирует пользователя в системе по <see cref="AuthJwtResponse.RefreshToken"/> и возвращает JWT-токен для аутентификации.
    /// </summary>
    /// <remarks>
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="refreshToken"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Токен не найден | Срок действия токена истёк</term>
    /// <description><see cref="ErrorMessages.InvalidToken"/>.</description>
    /// </item>
    /// <item>
    /// <term>Пользователь не найден</term>
    /// <description><see cref="ErrorMessages.UserNotFound"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="refreshToken"><see cref="AuthJwtResponse.RefreshToken"/>.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="refreshToken"/> <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns>Результат сервиса <see cref="ServiceResult{AuthJwtResponse}"/> с токенами аутентификации.</returns>
    Task<ServiceResult<AuthJwtResponse>> LoginAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Аутентифицирует пользователя в системе по <see cref="OpenIdUserInfo.Email"/> и возвращает JWT-токен для аутентификации.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="userInfo"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
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
    /// <param name="userInfo">Информация о пользователе OpenId.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns>Результат сервиса <see cref="ServiceResult{AuthJwtResponse}"/> с токенами аутентификации.</returns>
    Task<ServiceResult<AuthJwtResponse>> LoginAsync(OpenIdUserInfo userInfo, CancellationToken ct = default);

    /// <summary>
    /// Регистрирует пользователя в системе и возвращает JWT-токен для аутентификации.
    /// </summary>
    /// <remarks>
    /// <para>Вызывающий метод должен предоставить валидные, не пустые данные для <paramref name="createUserDto"/>.</para>
    /// <para>Для валидации <paramref name="createUserDto"/> используется <see cref="IValidator{CreateUserDto}"/>.</para>
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
    /// <term>Если возник конфликт параллельности</term>
    /// <description>исключение <see cref="DbUpdateConcurrencyException"/> | <see cref="DbUpdateException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Все ошибки из</term>
    /// <description><see cref="UserManager.CreateUserAsync(CreateUserDto)"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="createUserDto">DTO-модель для создания пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="createUserDto"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="createUserDto"/> невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns>Результат сервиса <see cref="ServiceResult{AuthJwtResponse}"/> с токеном аутентификации.</returns>
    Task<ServiceResult<AuthJwtResponse>> RegisterAsync(CreateUserDto createUserDto, CancellationToken ct = default);

    /// <summary>
    /// Регистрирует пользователя в системе через OAuth и возвращает JWT-токен для аутентификации.
    /// </summary>
    /// <remarks>
    /// <para>Вызывающий метод должен предоставить валидные, не пустые данные для <paramref name="oAuthCompleteRegistrationDto"/>.</para>
    /// <para>Для валидации <paramref name="oAuthCompleteRegistrationDto"/> используется <see cref="IValidator{OAuthCompleteRegistrationDto}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="userInfo"/> или <paramref name="oAuthCompleteRegistrationDto"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="oAuthCompleteRegistrationDto"/> невалидна</term>
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
    /// <term>Все ошибки из</term>
    /// <description><see cref="UserManager.CreateUserAsync(OpenIdUserInfo, OAuthCompleteRegistrationDto, CancellationToken)"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userInfo">Информация о пользователе OpenId.</param>
    /// <param name="oAuthCompleteRegistrationDto">DTO-модель завершения регистрации через OAuth.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="userInfo"/> или <paramref name="oAuthCompleteRegistrationDto"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="oAuthCompleteRegistrationDto"/> невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns>Результат сервиса <see cref="ServiceResult{AuthJwtResponse}"/> с токеном аутентификации.</returns>
    Task<ServiceResult<AuthJwtResponse>> RegisterAsync(OpenIdUserInfo userInfo, OAuthCompleteRegistrationDto oAuthCompleteRegistrationDto, CancellationToken ct = default);

    /// <summary>
    /// Отправляет на электронную почту письмо для подтверждения почты пользователя.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если после изменений данных сущности <see cref="ConfirmEmailRequest"/>, сущность окажется невалидна, изменения не последуют</term>
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
    /// <item>
    /// <term>Пользователь уже подтвердил электронную почту</term>
    /// <description><see cref="ErrorMessages.UserAlreadyConfirmedEmail"/>.</description>
    /// </item>
    /// <item>
    /// <term>Письмо уже отправлено</term>
    /// <description><see cref="ErrorMessages.LetterAlreadySent"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/> или если после изменений данных сущности <see cref="ConfirmEmailRequest"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"></see> результат сервиса.</returns>
    Task<ServiceResult> SendConfirmEmailAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Отправляет на телефонный номер сообщение для подтверждения.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если после изменений данных сущности <see cref="VerificationPhoneNumberRequest"/>, сущность окажется невалидна, изменения не последуют</term>
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
    /// <item>
    /// <term>Телефонный номер пользователя уже подтверждён</term>
    /// <description><see cref="ErrorMessages.UserAlreadyConfirmedPhoneNumber"/>.</description>
    /// </item>
    /// <item>
    /// <term>Код уже отправлен</term>
    /// <description><see cref="ErrorMessages.CodeAlreadySent"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="isTelegram">Отправить ли сообщение по Телеграму.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/> или если после изменений данных сущности <see cref="VerificationPhoneNumberRequest"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"></see> результат сервиса.</returns>
    Task<ServiceResult> SendVerificationCodePhoneNumberAsync(Guid userId, bool isTelegram, CancellationToken ct = default);
}