using CRUD.Models.Dtos.Notification;

namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="GetUserNotificationsDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public sealed class GetUserNotificationsDtoValidator : AbstractValidator<GetUserNotificationsDto>
{
    public GetUserNotificationsDtoValidator(IValidatorLocalizer localizer)
    {
        RuleFor(x => x.Count).Count(1, 100, localizer);
    }
}