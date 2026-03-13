namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для работы с запросами на верификацию телефонного номера.
/// </summary>
public interface IVerificationPhoneNumberRequestManager
{
    /// <summary>
    /// Добавляет сгенерированный код в базу данных и отправляет СМС.
    /// </summary>
    /// <remarks>
    /// <para>Для валидации используется <see cref="IValidator{VerificationPhoneNumberRequest}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="phoneNumber"/> или <paramref name="languageCode"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если после изменений данных сущности <see cref="VerificationPhoneNumberRequest"/>, сущность окажется невалидна, изменения не последуют</term>
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
    /// <term>Код уже отправлен</term>
    /// <description><see cref="ErrorMessages.CodeAlreadySent"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="phoneNumber">Телефонный номер пользователя.</param>
    /// <param name="languageCode">Код языка пользователя.</param>
    /// <param name="isTelegram">Отправить ли код через Телеграм.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="phoneNumber"/> или <paramref name="languageCode"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/> или если после изменений данных сущности <see cref="VerificationPhoneNumberRequest"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns>Результат сервиса <see cref="ServiceResult"/>.</returns>
    Task<ServiceResult> AddCodeToDatabaseAndSendSmsAsync(Guid userId, string phoneNumber, string languageCode, bool isTelegram = true, CancellationToken ct = default);
}