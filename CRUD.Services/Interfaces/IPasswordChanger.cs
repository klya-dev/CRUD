using CRUD.Models.Dtos.Password;

namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для работы со сменой пароля.
/// </summary>
public interface IPasswordChanger
{
    /// <summary>
    /// Валидирует пароль и отправляет письмо для смены пароля на электронную почту пользователя.
    /// </summary>
    /// <remarks>
    /// <para>Вызывающий метод должен предоставить валидные, не пустые данные для <paramref name="changePasswordDto"/>.</para>
    /// <para>Для валидации <paramref name="changePasswordDto"/> используется <see cref="IValidator{ChangePasswordDto}"/>.</para>
    /// <para>Для валидации <see cref="User"/> используется <see cref="IValidator{User}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="changePasswordDto"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="changePasswordDto"/> невалидна</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если после изменений данных сущности <see cref="User"/> или <see cref="ChangePasswordRequest"/>, сущность окажется невалидна, изменения не последуют</term>
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
    /// <item>
    /// <term>Письмо уже отправлено</term>
    /// <description><see cref="ErrorMessages.LetterAlreadySent"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="changePasswordDto">DTO-модель изменения пароля пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="changePasswordDto"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/> или если <paramref name="changePasswordDto"/> невалиден или если после изменений данных сущности <see cref="User"/> или <see cref="ChangePasswordRequest"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/> результат сервиса.</returns>
    Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto, CancellationToken ct = default);

    /// <summary>
    /// Подтверждает смену пароля и изменяет пароль пользователя в базе.
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
    /// </list>
    /// 
    /// </remarks>
    /// <param name="token">Токен для смены пароля.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="token"/> <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/> результат сервиса.</returns>
    Task<ServiceResult> ChangePasswordAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Меняет пароль пользователя в базе.
    /// </summary>
    /// <remarks>
    /// <para>Вызывающий метод должен предоставить валидные, не пустые данные для <paramref name="setPasswordDto"/>.</para>
    /// <para>Для валидации <see cref="SetPasswordDto"/> используется <see cref="IValidator{SetPasswordDto}"/>.</para>
    /// <para>Для валидации <see cref="User"/> используется <see cref="IValidator{User}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="setPasswordDto"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="setPasswordDto"/> невалидна</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если после изменений данных сущности <see cref="User"/> или <see cref="SetPasswordDto"/>, сущность окажется невалидна, изменения не последуют</term>
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
    /// <param name="setPasswordDto">DTO-модель изменения пароля пользователя.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="setPasswordDto"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/> или если <paramref name="setPasswordDto"/> невалиден или если после изменений данных сущности <see cref="User"/> или <see cref="SetPasswordRequest"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/> результат сервиса.</returns>
    Task<ServiceResult> SetPasswordAsync(Guid userId, SetPasswordDto setPasswordDto, CancellationToken ct = default);
}