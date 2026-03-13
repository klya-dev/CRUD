namespace CRUD.Utility;

/// <summary>
/// Константы с ключами для кэширования.
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// Является ли пользователь автором публикации.
    /// </summary>
    /// <remarks>
    /// <c>IsAuthorThisPublication-userId:publicationId</c>
    /// </remarks>
    public const string IsAuthorThisPublication = "IsAuthorThisPublication";

    /// <summary>
    /// OAuth AccessToken MailRu.
    /// </summary>
    /// <remarks>
    /// <c>OAuthAccessTokenMailRu-state</c>
    /// </remarks>
    public const string OAuthAccessTokenMailRu = "OAuthAccessTokenMailRu";

    /// <summary>
    /// Использованный идемпотентный ключ (<c>Idempotency-Key</c>).
    /// </summary>
    /// <remarks>
    /// <c>Idempotency-idempotencyKey</c>
    /// </remarks>
    public const string Idempotency = "Idempotency";
}