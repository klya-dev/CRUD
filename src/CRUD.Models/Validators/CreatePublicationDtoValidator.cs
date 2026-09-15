namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="CreatePublicationDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public sealed class CreatePublicationDtoValidator : AbstractValidator<CreatePublicationDto>
{
    public CreatePublicationDtoValidator(IValidatorLocalizer localizer)
    {
        RuleFor(x => x.Title).Title(localizer);
        RuleFor(x => x.Content).Content(localizer);
    }
}