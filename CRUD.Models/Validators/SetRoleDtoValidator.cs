namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="SetRoleDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public class SetRoleDtoValidator : AbstractValidator<SetRoleDto>
{
    public SetRoleDtoValidator(IValidatorsLocalizer localizer)
    {
        RuleFor(x => x.Role).Role(localizer);
    }
}