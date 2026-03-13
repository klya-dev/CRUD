namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="ChangePasswordDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordDtoValidator(IValidatorsLocalizer localizer)
    {
        RuleFor(x => x.Password).Password(localizer);
        RuleFor(x => x.NewPassword).NewPassword(localizer);
    }
}