using CRUD.Models.Dtos.Notification;

namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="CreateNotificationSelectedUsersDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public class CreateNotificationSelectedUsersDtoValidator : AbstractValidator<CreateNotificationSelectedUsersDto>
{
    public CreateNotificationSelectedUsersDtoValidator(IValidatorsLocalizer localizer)
    {
        RuleFor(x => x.UserIds).NotEmpty();
        RuleFor(x => x.Notification.Title).NotEmpty().Length(3, 48).WithName(localizer[ValidatorsLocalizerConstants.PropertyTitle]);
        RuleFor(x => x.Notification.Content).NotEmpty().Length(3, 96).WithName(localizer[ValidatorsLocalizerConstants.PropertyContent]);
    }
}