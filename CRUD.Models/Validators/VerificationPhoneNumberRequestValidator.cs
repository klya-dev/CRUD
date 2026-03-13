using Microsoft.Extensions.Options;

namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="VerificationPhoneNumberRequest"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public class VerificationPhoneNumberRequestValidator : AbstractValidator<Domains.VerificationPhoneNumberRequest>
{
    public VerificationPhoneNumberRequestValidator(IOptions<VerificationPhoneNumberRequestOptions> options)
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().Length(options.Value.LengthCode);
        RuleFor(x => x.CreatedAt).NotEmpty();
        RuleFor(x => x.Expires).NotEmpty();
    }
}