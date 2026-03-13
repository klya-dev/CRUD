namespace CRUD.Utility.Options;

/// <summary>
/// Опции, используемые для подтверждения смены пароля пользователя.
/// </summary>
public class ChangePasswordRequestOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "Requests:ChangePasswordRequest";

    /// <summary>
    /// Через сколько истекает токен.
    /// </summary>
    public required TimeSpan Expires { get; set; }

    /// <summary>
    /// Через сколько можно отправить запрос повторно.
    /// </summary>
    public required TimeSpan Timeout { get; set; }
}