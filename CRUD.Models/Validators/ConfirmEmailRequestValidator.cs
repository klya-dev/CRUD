namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="ConfirmEmailRequest"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public class ConfirmEmailRequestValidator : AbstractValidator<Domains.ConfirmEmailRequest>
{
    public ConfirmEmailRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Token).Token();
        RuleFor(x => x.CreatedAt).NotEmpty();
        RuleFor(x => x.Expires).NotEmpty();
    }
}