namespace CRUD.Utility.Options;

/// <summary>
/// Опции, используемые для верификации телефонного номера.
/// </summary>
public class VerificationPhoneNumberRequestOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "Requests:VerificationPhoneNumberRequest";

    /// <summary>
    /// Через сколько истекает код.
    /// </summary>
    public required TimeSpan Expires { get; set; }

    /// <summary>
    /// Через сколько можно отправить код повторно.
    /// </summary>
    public required TimeSpan Timeout { get; set; }

    /// <summary>
    /// Длина кода.
    /// </summary>
    /// <remarks>
    /// Это значение указывается не только для генерации, но и для валидатора.
    /// </remarks>
    public required int LengthCode { get; set; }
}