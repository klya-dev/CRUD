namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="GetPaginatedListDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public sealed class GetPaginatedListDtoValidator : AbstractValidator<GetPaginatedListDto>
{
    public GetPaginatedListDtoValidator(IValidatorLocalizer localizer)
    {
        RuleFor(x => x.PageIndex).NotEmpty().GreaterThanOrEqualTo(1).WithName(localizer[ValidatorLocalizerConstants.PropertyPageIndex]);
        RuleFor(x => x.PageSize).NotEmpty().InclusiveBetween(1, 25).WithName(localizer[ValidatorLocalizerConstants.PropertyPageSize]);
    }
}