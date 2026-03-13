namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис (провайдер) для работы с OAuth MailRu.
/// </summary>
/// <remarks>
/// <seealso href="https://id.vk.com/about/business/go/docs/ru/vkid/latest/oauth/oauth-mail/index"/>, <seealso href="https://o2.mail.ru/app/"/>.
/// </remarks>
public interface IOAuthMailRuProvider
{
    /// <summary>
    /// Возвращает ссылку для входа пользователя в аккаунт MailRu. 
    /// </summary>
    /// <remarks>
    /// <para>Если не удалось — <see langword="null"/>.</para>
    /// </remarks>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns>Ссылка для входа, если удалось получить, иначе <see langword="null"/>.</returns>
    Task<string?> GetAuthorizationLinkAsync(CancellationToken ct = default);

    /// <summary>
    /// Возвращает AccessToken MailRu по авторизационному коду и строке состояния.
    /// </summary>
    /// <remarks>
    /// <para>Если не удалось — <see langword="null"/>.</para>
    /// </remarks>
    /// <param name="code">Авторизационный код.</param>
    /// <param name="state">Строка состояния.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns>AccessToken, если удалось получить, иначе <see langword="null"/>.</returns>
    Task<string?> GetAccessTokenAsync(string code, string state, CancellationToken ct = default);

    /// <summary>
    /// Возвращает <see cref="OpenIdUserInfo"/> по AccessToken'у MailRu.
    /// </summary>
    /// <remarks>
    /// <para>Если не удалось — <see langword="null"/>.</para>
    /// </remarks>
    /// <param name="accessToken">AccessToken MailRu.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="OpenIdUserInfo"/>, если удалось получить, иначе <see langword="null"/>.</returns>
    Task<OpenIdUserInfo?> GetUserInfoAsync(string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Возвращает <see cref="OpenIdConfiguration"/>.
    /// </summary>
    /// <remarks>
    /// <para>Если не удалось — <see langword="null"/>.</para>
    /// </remarks>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="OpenIdConfiguration"/>, если удалось получить, иначе <see langword="null"/>.</returns>
    Task<OpenIdConfiguration?> GetOpenIdConfiguration(CancellationToken ct = default);
}