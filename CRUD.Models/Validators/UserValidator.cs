namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="User"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public class UserValidator : AbstractValidator<Domains.User>
{
    public UserValidator()
    {
        // Т.к это валидация domain модели, и в случаи невалидных данных в сервисе выбрасывается исключение, а ошибки должны быть на английском языке. Это не DTO'шка!

        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Firstname).Firstname();
        RuleFor(x => x.Username).Username();
        RuleFor(x => x.HashedPassword).HashedPassword();
        RuleFor(x => x.LanguageCode).LanguageCode();
        RuleFor(x => x.Role).Role();
        RuleFor(x => x.IsPremium);
        RuleFor(x => x.ApiKey).ApiKey();
        RuleFor(x => x.DisposableApiKey).DisposableApiKey();
        RuleFor(x => x.Email).Email();
        RuleFor(x => x.PhoneNumber).PhoneNumber();
    }
}