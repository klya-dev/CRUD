namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для работы с запросами на подтверждение смены пароля.
/// </summary>
public interface IChangePasswordRequestManager
{
    /// <summary>
    /// Добавляет сгенерированный токен в хранилище и отправляет письмо.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="email"/> или <paramref name="languageCode"/>  или <paramref name="newHashedPassword"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
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
    /// <param name="email">Электронная почта пользователя.</param>
    /// <param name="languageCode">Код языка пользователя.</param>
    /// <param name="newHashedPassword">Хэшированный новый пароль пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="email"/> или <paramref name="languageCode"/>  или <paramref name="newHashedPassword"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns>Результат сервиса <see cref="ServiceResult"/>.</returns>
    Task<ServiceResult> AddTokenToStorageAndSendLetterAsync(Guid userId, string email, string languageCode, string newHashedPassword, CancellationToken ct = default);
}