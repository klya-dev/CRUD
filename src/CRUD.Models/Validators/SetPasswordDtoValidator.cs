namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="SetPasswordDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public sealed class SetPasswordDtoValidator : AbstractValidator<SetPasswordDto>
{
    public SetPasswordDtoValidator(IValidatorLocalizer localizer)
    {
        RuleFor(x => x.NewPassword).NewPassword(localizer);
    }
}