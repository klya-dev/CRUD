namespace CRUD.WebApi.Policies;

/// <summary>
/// Коллекция запрещённых языков (требование).
/// </summary>
public sealed class LanguageDenyRequirement : IAuthorizationRequirement
{
    // Чтобы использовать, как атрибут или в MinimalApi .RequireAuthorization(new LanguageDenyRequirement("some"));
    // Нужно реализовать IAuthorizationRequirementData | https://learn.microsoft.com/ru-ru/aspnet/core/security/authorization/custom-authorization-policies-with-iauthorizationrequirementdata
    // Я использую на основе политик https://learn.microsoft.com/ru-ru/aspnet/core/security/authorization/policies#requirements

    /// <summary>
    /// Конструктор создания запрещённого языка.
    /// </summary>
    /// <param name="languageCodes">Код языка.</param>
    public LanguageDenyRequirement(string languageCode) => LanguageCodes = [languageCode];

    /// <summary>
    /// Конструктор создания запрещённых языков.
    /// </summary>
    /// <param name="languageCodes">Коды языков.</param>
    public LanguageDenyRequirement(IEnumerable<string> languageCodes) => LanguageCodes = languageCodes;

    /// <summary>
    /// Запрещенные коды языков.
    /// </summary>
    public IEnumerable<string> LanguageCodes { get; }
}