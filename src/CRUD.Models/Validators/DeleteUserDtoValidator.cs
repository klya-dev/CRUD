namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="DeleteUserDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public sealed class DeleteUserDtoValidator : AbstractValidator<DeleteUserDto>
{
    public DeleteUserDtoValidator(IValidatorLocalizer localizer)
    {
        RuleFor(x => x.Password).Password(localizer);
    }
}