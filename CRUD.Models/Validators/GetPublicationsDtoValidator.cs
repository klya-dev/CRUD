namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="GetPublicationsDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public class GetPublicationsDtoValidator : AbstractValidator<GetPublicationsDto>
{
    public GetPublicationsDtoValidator(IValidatorsLocalizer localizer)
    {
        RuleFor(x => x.Count).Count(1, 100, localizer);
    }
}