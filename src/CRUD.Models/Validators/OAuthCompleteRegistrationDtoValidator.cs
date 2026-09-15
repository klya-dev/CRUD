namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="OAuthCompleteRegistrationDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public sealed class OAuthCompleteRegistrationDtoValidator : AbstractValidator<OAuthCompleteRegistrationDto>
{
    public OAuthCompleteRegistrationDtoValidator(IValidatorLocalizer localizer)
    {
        RuleFor(x => x.PhoneNumber).PhoneNumber(localizer);
    }
}