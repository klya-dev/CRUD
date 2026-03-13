namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="LoginDataDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public class LoginDataDtoValidator : AbstractValidator<LoginDataDto>
{
    public LoginDataDtoValidator(IValidatorsLocalizer localizer)
    {
        RuleFor(x => x.Username).NotEmpty(); // Пользователь также как с паролем, может зарегаться, а потом API может поменять правила, так что, лишь бы не пустое поле
        RuleFor(x => x.Password).Password(localizer);
    }
}