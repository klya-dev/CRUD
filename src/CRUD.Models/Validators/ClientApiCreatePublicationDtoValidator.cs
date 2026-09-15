namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор класса <see cref="ClientApiCreatePublicationDto"/>.
/// </summary>
/// <remarks>
/// Валидация реализована через Fluent Validation.
/// </remarks>
public sealed class ClientApiCreatePublicationDtoValidator : AbstractValidator<ClientApiCreatePublicationDto>
{
    public ClientApiCreatePublicationDtoValidator(IValidatorLocalizer localizer)
    {
        RuleFor(x => x.Title).Title(localizer);
        RuleFor(x => x.Content).Content(localizer);
        RuleFor(x => x.ApiKey).ApiKeyOrDisposableApiKey(localizer);
    }
}