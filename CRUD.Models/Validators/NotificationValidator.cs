namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="Notification"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public class NotificationValidator : AbstractValidator<Notification>
{
    public NotificationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().Length(3, 48);
        RuleFor(x => x.Content).NotEmpty().Length(3, 96);
        RuleFor(x => x.CreatedAt).NotEmpty();
    }
}