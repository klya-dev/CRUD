using OpenTelemetry.Metrics;

namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> расширения OpenTelemetry.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Добавляет и настраивает OpenTelemetry.
    /// </summary>
    public static IServiceCollection AddCustomOpenTelemetry(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithMetrics(builder =>
            {
                builder.AddPrometheusExporter();

                // https://learn.microsoft.com/ru-ru/aspnet/core/log-mon/metrics/built-in?view=aspnetcore-10.0
                builder.AddMeter(
                    "Microsoft.AspNetCore.Hosting",
                    "Microsoft.AspNetCore.Diagnostics",
                    "Microsoft.AspNetCore.RateLimiting",
                    "Microsoft.AspNetCore.Server.Kestrel",
                    "Microsoft.AspNetCore.Http.Connections",
                    ApiMeters.MeterName);

                // Grpc.Net.Client метрики (в отличии от трассировок) используют EventCounter, поэтому просто добавить в AddMeter не выйдет (https://github.com/open-telemetry/opentelemetry-dotnet-contrib/issues/1618)
                builder.AddEventCountersInstrumentation(options =>
                {
                    options.AddEventSources("Grpc.Net.Client");
                });
            });

        return services;
    }
}