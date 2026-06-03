namespace CRUD.WebApi.Policies;

/// <summary>
/// Обработчик запрещённых языков в контексте авторизации.
/// </summary>
public sealed class LanguageDenyHandler : AuthorizationHandler<LanguageDenyRequirement>
{
    private readonly ILogger<LanguageDenyHandler> _logger;
    
    public LanguageDenyHandler(ILogger<LanguageDenyHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LanguageDenyRequirement requirement)
    {
        _logger.LogDebug("Requirements: {requirements}", context.Requirements);

        // Ищем код языка в claim'ах
        var languageCode = context.User.FindFirstValue(UserClaimTypes.LanguageCode);
        if (languageCode != null && !requirement.LanguageCodes.Any(x => x == languageCode)) // Полученного кода языка из claim'ов нет в списке запрещённых
            context.Succeed(requirement); // Требование проверено, можно идти дальше
        else // Полученный код языка есть в списке запрещённых
        {
            _logger.LogDebug("Попытка выполнения запроса пользователем с запрещённым языком: \"{languageCode}\".", languageCode);
            context.Fail();
            return Task.CompletedTask;
        }

        return Task.CompletedTask;

        // context.Succeed: успех, запрос соответствует требованиям, можно переходить к следующему обработчику
        // context.Fail: провал, даже если перед этим обработчиком было 10 успешных, обработчик вернёт провал, НО при этом следующие обработчики будут выполняться (т.к по умолчанию InvokeHandlersAfterFailure = true https://learn.microsoft.com/ru-ru/aspnet/core/security/authorization/policies?view=aspnetcore-10.0#what-should-a-handler-return)
        // Task.CompletedTask: подходит, например, если данные не удалось проверить - их нет. Т.е обработчик вернул не успех и не провал, а просто переход к следующему обработчику
    }
}