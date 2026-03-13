namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="Product"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public class ProductValidator : AbstractValidator<Domains.Product>
{
    public ProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Price).NotEmpty().InclusiveBetween(1, 100_000); // От 1 до 100 000
    }
}