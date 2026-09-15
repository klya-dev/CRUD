namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="GetCursorPaginatedListDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public sealed class GetCursorPaginatedListDtoValidator : AbstractValidator<GetCursorPaginatedListDto>
{
    public GetCursorPaginatedListDtoValidator()
    {
        // localizer лень прокидывать и добавлять новые константы

        RuleFor(x => x.Date).NotEqual(DateTime.MinValue);
        RuleFor(x => x.LastId).NotEqual(Guid.Empty);
        RuleFor(x => x.Limit).NotEmpty().InclusiveBetween(1, 25);
    }
}