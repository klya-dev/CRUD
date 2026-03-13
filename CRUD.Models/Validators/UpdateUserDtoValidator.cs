namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="UpdateUserDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator(IValidatorsLocalizer localizer)
    {
        RuleFor(x => x.Firstname).Firstname(localizer);
        RuleFor(x => x.Username).Username(localizer);
        RuleFor(x => x.LanguageCode).LanguageCode(localizer);
    }
}