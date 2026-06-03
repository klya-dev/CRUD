namespace CRUD.Utility;

/// <summary>
/// Кастомные типы Claim'ов пользователя.
/// </summary>
public static class UserClaimTypes
{
    /// <summary>
    /// Код языка.
    /// </summary>
    public const string LanguageCode = "language_code";

    /// <summary>
    /// Подтверждена ли почта.
    /// </summary>
    public const string IsEmailConfirm = "email_confirm";

    /// <summary>
    /// Подтверждён ли номер телефона.
    /// </summary>
    public const string IsPhoneNumberConfirm = "phonenumber_confirm";

    /// <summary>
    /// Есть ли у пользователя премиум.
    /// </summary>
    public const string IsPremium = "premium";
}