namespace CRUD.Utility.Options;

/// <summary>
/// Параметры EmailSender аутентификации/авторизации.
/// </summary>
public class AuthEmailSenderOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "AuthEmailSender";

    /// <summary>
    /// Потребитель токена.
    /// </summary>
    /// <remarks>
    /// <para>Кому выдан токен сервер авторизации?</para>
    /// <para>Если не очень понятно с моим примером - монолитом + микросервис. То можно представить: если бы у меня был отдельный сервер авторизации, то кто был бы потребителем? Правильно, микросервис EmailSender.</para>
    /// </remarks>
    public required string Audience { get; set; }

    /// <summary>
    /// Через сколько истекает Access-токен.
    /// </summary>
    public required TimeSpan Expires { get; set; }
}