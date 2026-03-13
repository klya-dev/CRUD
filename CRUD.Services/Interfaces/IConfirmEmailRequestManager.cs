namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для работы с запросами на подтверждение электронной почты.
/// </summary>
public interface IConfirmEmailRequestManager
{
    /// <summary>
    /// Добавляет сгенерированный токен в базу данных и отправляет письмо с подтверждением.
    /// </summary>
    /// <remarks>
    /// <para>Для валидации используется <see cref="IValidator{ConfirmEmailRequest}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="email"/> или <paramref name="languageCode"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если после изменений данных сущности <see cref="ConfirmEmailRequest"/>, сущность окажется невалидна, изменения не последуют</term>
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
    /// <term>Письмо уже отправлено</term>
    /// <description><see cref="ErrorMessages.LetterAlreadySent"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="email">Email пользователя.</param>
    /// <param name="languageCode">Код языка пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="email"/> или <paramref name="languageCode"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/> или если после изменений данных сущности <see cref="ConfirmEmailRequest"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns>Результат сервиса <see cref="ServiceResult"/>.</returns>
    Task<ServiceResult> AddTokenToDatabaseAndSendLetterAsync(Guid userId, string email, string languageCode, CancellationToken ct = default);
}