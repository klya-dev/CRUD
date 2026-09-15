namespace CRUD.WebApi.Helpers;

/// <summary>
/// Имена политик авторизации.
/// </summary>
public static class AuthorizationPolicyNames
{
    /// <summary>
    /// Только пользователи с ролью <see cref="UserRoles.Admin"/>.
    /// </summary>
    public const string OnlyAdmin = "OnlyAdmin";

    /// <summary>
    /// Только пользователи с премиумом.
    /// </summary>
    public const string OnlyPremium = "OnlyPremium";

    /// <summary>
    /// Только пользователи с подтверждённой почтой.
    /// </summary>
    public const string OnlyEmailConfirmed = "OnlyEmailConfirmed";

    /// <summary>
    /// Только пользователи с подтверждённым номером телефона.
    /// </summary>
    public const string OnlyPhoneNumberConfirmed = "OnlyPhoneNumberConfirmed";

    /// <summary>
    /// Только разрешённые языки.
    /// </summary>
    /// <remarks>
    /// Языки, которые не указаны в <see cref="LanguageDenyRequirement"/>.
    /// </remarks>
    public const string OnlyPermittedLanguages = "OnlyPermittedLanguages";
}