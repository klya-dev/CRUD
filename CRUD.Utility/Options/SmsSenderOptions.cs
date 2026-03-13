namespace CRUD.Utility.Options;

/// <summary>
/// Опции SmsSender'а.
/// </summary>
public class SmsSenderOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "SmsSender";

    /// <summary>
    /// URL сервиса (шлюза).
    /// </summary>
    public required string ServiceURL { get; set; }

    /// <summary>
    /// Электронная почта зарегистрированная на сервисе.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// API-ключ.
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// Отображаемое имя в СМС.
    /// </summary>
    public required string Sign { get; set; }
}