namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="SetPasswordDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public class SetPasswordDtoValidator : AbstractValidator<SetPasswordDto>
{
    public SetPasswordDtoValidator(IValidatorsLocalizer localizer)
    {
        RuleFor(x => x.NewPassword).NewPassword(localizer);
    }
}