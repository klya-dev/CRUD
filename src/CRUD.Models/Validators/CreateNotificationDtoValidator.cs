using CRUD.Models.Dtos.Notification;

namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="CreateNotificationDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public sealed class CreateNotificationDtoValidator : AbstractValidator<CreateNotificationDto>
{
    public CreateNotificationDtoValidator(IValidatorLocalizer localizer)
    {
        RuleFor(x => x.Title).NotEmpty().Length(3, 48).WithName(localizer[ValidatorLocalizerConstants.PropertyTitle]);
        RuleFor(x => x.Content).NotEmpty().Length(3, 96).WithName(localizer[ValidatorLocalizerConstants.PropertyContent]);
    }
}