namespace CRUD.WebApi.Policies;

public class LanguageDenyHandler : AuthorizationHandler<LanguageDeny>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LanguageDeny requirement)
    {
        var languageClaim = context.User.FindFirst(c => c.Type == "language_code");
        if (languageClaim is not null)
        {
            if (languageClaim.Value != requirement.LanguageCode)
                context.Succeed(requirement); // Сигнализируем, что claim соответствует ограничению
        }

        return Task.CompletedTask;
    }
}