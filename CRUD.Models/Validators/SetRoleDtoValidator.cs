namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="SetRoleDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public sealed class SetRoleDtoValidator : AbstractValidator<SetRoleDto>
{
    public SetRoleDtoValidator(IValidatorLocalizer localizer)
    {
        RuleFor(x => x.Role).Role(localizer);
    }
}