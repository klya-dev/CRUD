namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="CreateUserDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator(IValidatorsLocalizer localizer)
    {
        RuleFor(x => x.Firstname).Firstname(localizer);
        RuleFor(x => x.Username).Username(localizer);
        RuleFor(x => x.Password).NewPassword(localizer, displayPasswordName: true); // Отображаемое имя "Пароль"
        RuleFor(x => x.LanguageCode).LanguageCode(localizer);
        RuleFor(x => x.Email).Email(localizer);
        RuleFor(x => x.PhoneNumber).PhoneNumber(localizer);
    }
}