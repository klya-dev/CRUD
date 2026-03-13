namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="Domains.Publication"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public class PublicationValidator : AbstractValidator<Domains.Publication>
{
    public PublicationValidator()
    {
        // Т.к это валидация domain модели, и в случаи невалидных данных в сервисе выбрасывается исключение, а ошибки должны быть на английском языке. Это не DTO'шка!

        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CreatedAt).NotEmpty();
        RuleFor(x => x.EditedAt).NotEqual(DateTime.MinValue);
        RuleFor(x => x.Title).Title();
        RuleFor(x => x.Content).Content();
        RuleFor(x => x.AuthorId).NotEqual(Guid.Empty); // Когда AuthorId был не nullable, NotEmpty считался, как Guid.Empty, но после того, как AuthorId стал nullable, то дефолт для типа стал null, поэтому Guid.Empty нужно проверять отдельно
    }
}