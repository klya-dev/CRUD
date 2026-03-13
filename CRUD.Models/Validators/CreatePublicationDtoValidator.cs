namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="CreatePublicationDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public class CreatePublicationDtoValidator : AbstractValidator<CreatePublicationDto>
{
    public CreatePublicationDtoValidator(IValidatorsLocalizer localizer)
    {
        RuleFor(x => x.Title).Title(localizer);
        RuleFor(x => x.Content).Content(localizer);
    }
}