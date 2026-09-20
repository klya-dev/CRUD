namespace CRUD.WebApi.Extensions.Endpoints;

/// <summary>
/// <see cref="WebApplication"/> расширения прикладных конечных точек приложения.
/// </summary>
public static class ApplicationCoreEndpointsExtensions
{
    /// <summary>
    /// Мапит прикладные конечные точки приложения.
    /// </summary>
    public static WebApplication MapApplicationEndpoints(this WebApplication app)
    {
        var apiVersionSet = app.NewApiVersionSet()
            .HasDeprecatedApiVersion(new ApiVersion(1.0)) // Указываю, что v1 является устаревшим API
            .HasApiVersion(new ApiVersion(2.0)) // Поддерживаемая версия API
            .ReportApiVersions()
            .Build();

        AuthEndpoints.Map(app);
        AdminEndpoints.Map(app);
        UsersEndpoints.Map(app, apiVersionSet);
        UserEndpoints.Map(app, apiVersionSet);
        ConfirmationsEndpoints.Map(app, apiVersionSet);
        PublicationsEndpoints.Map(app, apiVersionSet);
        ClientApiEndpoints.Map(app, apiVersionSet);
        WebHooksEndpoints.Map(app);
        WellKnownEndpoints.Map(app);

        return app;
    }
}