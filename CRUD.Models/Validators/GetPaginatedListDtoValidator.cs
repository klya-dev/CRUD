namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="GetPaginatedListDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public class GetPaginatedListDtoValidator : AbstractValidator<GetPaginatedListDto>
{
    public GetPaginatedListDtoValidator(IValidatorsLocalizer localizer)
    {
        RuleFor(x => x.PageIndex).NotEmpty().GreaterThanOrEqualTo(1).WithName(localizer[ValidatorsLocalizerConstants.PropertyPageIndex]);
        RuleFor(x => x.PageSize).NotEmpty().InclusiveBetween(1, 25).WithName(localizer[ValidatorsLocalizerConstants.PropertyPageSize]);
    }
}