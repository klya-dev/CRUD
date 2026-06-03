using CRUD.Models.Dtos.Notification;

namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="CreateNotificationSelectedUsersDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public sealed class CreateNotificationSelectedUsersDtoValidator : AbstractValidator<CreateNotificationSelectedUsersDto>
{
    public CreateNotificationSelectedUsersDtoValidator(IValidatorLocalizer localizer)
    {
        RuleFor(x => x.UserIds).NotEmpty();
        RuleFor(x => x.Notification.Title).NotEmpty().Length(3, 48).WithName(localizer[ValidatorLocalizerConstants.PropertyTitle]);
        RuleFor(x => x.Notification.Content).NotEmpty().Length(3, 96).WithName(localizer[ValidatorLocalizerConstants.PropertyContent]);
    }
}