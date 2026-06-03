namespace CRUD.Utility.Options;

/// <summary>
/// Опции SmsSender'а.
/// </summary>
public sealed class SmsSenderOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "SmsSender";

    /// <summary>
    /// URL сервиса (шлюза).
    /// </summary>
    public required string ServiceURL { get; init; }

    /// <summary>
    /// Электронная почта зарегистрированная на сервисе.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// API-ключ.
    /// </summary>
    public required string ApiKey { get; init; }

    /// <summary>
    /// Отображаемое имя в СМС.
    /// </summary>
    public required string Sign { get; init; }
}