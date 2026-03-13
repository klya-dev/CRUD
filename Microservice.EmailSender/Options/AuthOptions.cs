namespace Microservice.EmailSender.Options;

/// <summary>
/// Опции аутентификации/авторизации.
/// </summary>
public class AuthOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "Auth";

    /// <summary>
    /// Издатель токена.
    /// </summary>
    /// <remarks>
    /// Обычно это сервер авторизации.
    /// </remarks>
    public required string Issuer { get; set; }

    /// <summary>
    /// Потребитель токена.
    /// </summary>
    /// <remarks>
    /// <para>Кому выдан токен сервер авторизации?</para>
    /// <para>Если не очень понятно с моим примером - монолитом + микросервис. То можно представить: если бы у меня был отдельный сервер авторизации, то кто был бы потребителем? Правильно, микросервис EmailSender.</para>
    /// </remarks>
    public required string Audience { get; set; }

    /// <summary>
    /// Url до "/.well-known/jwks.json" сервера авторизации.
    /// </summary>
    public required string JwksUrl { get; set; }

    // Т.к этот микросервис только валидирует токен, а не выдаёт, ему необязательно знать, когда истекает токен (asp.net сам проверяет, мне нигде указывать не нужно), а публичный ключ достаётся из "/.well-known/jwks.json" сервера авторизации
}