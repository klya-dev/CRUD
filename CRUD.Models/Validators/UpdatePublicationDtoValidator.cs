namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="UpdatePublicationDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public class UpdatePublicationDtoValidator : AbstractValidator<UpdatePublicationDto>
{
    public UpdatePublicationDtoValidator(IValidatorsLocalizer localizer)
    {
        RuleFor(x => x.PublicationId).NotEmpty();
        RuleFor(x => x.Title).Title(localizer, required: false);
        RuleFor(x => x.Content).Content(localizer, required: false);
    }
}