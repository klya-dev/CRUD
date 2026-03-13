namespace CRUD.Utility.Options;

/// <summary>
/// Параметры WebApi аутентификации/авторизации.
/// </summary>
public class AuthWebApiOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "AuthWebApi";

    /// <summary>
    /// Потребитель токена.
    /// </summary>
    /// <remarks>
    /// <para>Кому выдан токен сервер авторизации?</para>
    /// <para>Если не очень понятно с моим примером - монолитом. То можно представить: если бы у меня был отдельный сервер авторизации, то кто был бы потребителем? Правильно, WebApi.</para>
    /// </remarks>
    public required string Audience { get; set; }

    /// <summary>
    /// Через сколько истекает Access-токен.
    /// </summary>
    public required TimeSpan Expires { get; set; }

    /// <summary>
    /// Через сколько истекает Refresh-токен.
    /// </summary>
    public required TimeSpan ExpiresRefreshToken { get; set; }

    /// <summary>
    /// Максимальное количество Refresh-токенов у пользователя.
    /// </summary>
    /// <remarks>
    /// Обычно равняется количеству устройств пользователя (веб-сайт, десктопное и мобильное приложение).
    /// </remarks>
    public required int MaxCountRefreshTokens { get; set; }
}