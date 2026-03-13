namespace CRUD.WebApi.Policies;

public class LanguageDeny : IAuthorizationRequirement
{
    protected internal string LanguageCode { get; set; }
    public LanguageDeny(string languageCode) => LanguageCode = languageCode;
}