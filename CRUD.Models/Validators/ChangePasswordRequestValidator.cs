namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="ChangePasswordRequest"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public class ChangePasswordRequestValidator : AbstractValidator<Domains.ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.HashedNewPassword).HashedPassword();
        RuleFor(x => x.Token).Token();
        RuleFor(x => x.CreatedAt).NotEmpty();
        RuleFor(x => x.Expires).NotEmpty();
    }
}