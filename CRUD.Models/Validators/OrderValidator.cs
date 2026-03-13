namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="Order"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public class OrderValidator : AbstractValidator<Models.Domains.Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UserId);
        RuleFor(x => x.Status).OrderStatus();
        RuleFor(x => x.PaymentStatus).PaymentStatus();
        RuleFor(x => x.ProductName).ProductName();
        RuleFor(x => x.Paid);
        RuleFor(x => x.Amount).NotEmpty().InclusiveBetween(1, 100_000);
        RuleFor(x => x.Currency).NotEmpty();
        RuleFor(x => x.CreatedAt).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Refundable);
    }
}