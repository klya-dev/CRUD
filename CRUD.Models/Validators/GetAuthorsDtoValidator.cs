namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="GetAuthorsDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public class GetAuthorsDtoValidator : AbstractValidator<GetAuthorsDto>
{
    public GetAuthorsDtoValidator(IValidatorsLocalizer localizer)
    {
        RuleFor(x => x.Count).Count(1, 100, localizer);
    }
}