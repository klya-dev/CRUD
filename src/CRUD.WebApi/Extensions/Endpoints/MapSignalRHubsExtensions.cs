namespace CRUD.WebApi.Extensions.Endpoints;

/// <summary>
/// <see cref="WebApplication"/> расширения конечной точки SignalR.
/// </summary>
public static class MapSignalRHubsExtensions
{
    /// <summary>
    /// Мапит конечные точки хабов.
    /// </summary>
    public static WebApplication MapApplicationHubs(this WebApplication app)
    {
        app.MapHub<NotificationHub>("/notificationHub", options =>
        {
            options.AllowStatefulReconnects = true; // Если какие-то перебои, то сервер (и клиент) буферизирует данные и даёт возможность переподключится +withStatefulReconnect на клиенте
        }).RequireAuthorization();

        return app;
    }
}