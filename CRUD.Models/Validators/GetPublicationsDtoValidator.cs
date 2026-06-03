namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="GetPublicationsDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public sealed class GetPublicationsDtoValidator : AbstractValidator<GetPublicationsDto>
{
    public GetPublicationsDtoValidator(IValidatorLocalizer localizer)
    {
        RuleFor(x => x.Count).Count(1, 100, localizer);
    }
}