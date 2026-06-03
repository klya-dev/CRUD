namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="ChangePasswordDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public sealed class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordDtoValidator(IValidatorLocalizer localizer)
    {
        RuleFor(x => x.Password).Password(localizer);
        RuleFor(x => x.NewPassword).NewPassword(localizer);
    }
}