namespace CRUD.Utility;

/// <summary>
/// Форматы времени для <see cref="DateTime"/>.
/// </summary>
public static class DateTimeFormats
{
    /// <summary>
    /// Значение по умолчанию.
    /// </summary>
    public const string Default = "yyyy-MM-ddTHH:mm:ssZ";

    /// <summary>
    /// С тиками (миллисекунды, микросекунды, наносекунды).
    /// </summary>
    public const string WithTicks = "yyyy-MM-ddTHH:mm:ss.ffffffZ";

    /// <summary>
    /// Только дата.
    /// </summary>
    public const string DateOnly = "yyyy-MM-dd";

    /// <summary>
    /// Полная.
    /// </summary>
    public const string Full = "yyyy-MM-ddTHH:mm:ss.fffffffZ";

    /// <summary>
    /// UNIX.
    /// </summary>
    public const string UnixTimestamp = "Unix";
}