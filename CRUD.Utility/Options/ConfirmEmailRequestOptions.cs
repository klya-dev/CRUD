namespace CRUD.Utility.Options;

/// <summary>
/// Опции, используемые для подтверждения электронной почты.
/// </summary>
public sealed class ConfirmEmailRequestOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "Requests:ConfirmEmailRequest";

    /// <summary>
    /// Через сколько истекает токен.
    /// </summary>
    public required TimeSpan Expires { get; init; }

    /// <summary>
    /// Через сколько можно отправить запрос повторно.
    /// </summary>
    public required TimeSpan Timeout { get; init; }
}