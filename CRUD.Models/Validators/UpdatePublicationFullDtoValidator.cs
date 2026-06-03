namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="UpdatePublicationFullDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public sealed class UpdatePublicationFullDtoValidator : AbstractValidator<UpdatePublicationFullDto>
{
    public UpdatePublicationFullDtoValidator(IValidatorLocalizer localizer)
    {
        RuleFor(x => x.Title).Title(localizer, required: false);
        RuleFor(x => x.Content).Content(localizer, required: false);
        RuleFor(x => x.CreatedAt).DateTimeMatchFormat(DateTimeFormats.WithTicks, localizer); // Формат должен соответствовать DateTimeFormats.WithTicks
    }
}