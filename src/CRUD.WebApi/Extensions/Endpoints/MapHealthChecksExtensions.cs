namespace CRUD.WebApi.Extensions.Endpoints;

/// <summary>
/// <see cref="WebApplication"/> расширения конечной точки HealthCheck.
/// </summary>
public static class MapHealthChecksExtensions
{
    /// <summary>
    /// Мапит конечную точку HealthCheck (<c>/healthz</c>).
    /// </summary>
    public static WebApplication MapApplicationHealthCheck(this WebApplication app)
    {
        app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
        {
            AllowCachingResponses = false,

            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        })
        .RequireAuthorization(AuthorizationPolicyNames.OnlyAdmin) // С авторизацией админа
        .DisableHttpMetrics(); // Без метрик

        return app;
    }
}