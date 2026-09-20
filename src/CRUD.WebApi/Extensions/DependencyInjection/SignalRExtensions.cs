namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> расширения SignalR.
/// </summary>
public static class SignalRExtensions
{
    /// <summary>
    /// Добавляет и настраивает SignalR.
    /// </summary>
    /// <remarks>
    /// Для мапинга хаба используй <see cref="MapSignalRHubsExtensions.MapApplicationHubs(WebApplication)"/>.
    /// </remarks>
    public static IServiceCollection AddCustomSignalR(this IServiceCollection services)
    {
        services.AddSignalR()
            .AddMessagePackProtocol();
        // Добавляем поддержку MessagePack протокола.
        // По дефолту JSON уже есть. MessagePack протокол быстрее, чем Json (https://learn.microsoft.com/ru-ru/aspnet/core/signalr/messagepackhubprotocol?view=aspnetcore-10.0)

        return services;
    }
}