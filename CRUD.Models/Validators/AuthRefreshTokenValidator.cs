namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="AuthRefreshToken"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public class AuthRefreshTokenValidator : AbstractValidator<AuthRefreshToken>
{
    public AuthRefreshTokenValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Token).Token();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Expires).NotEmpty();
    }
}