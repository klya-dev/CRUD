namespace CRUD.WebApi.Extensions.Middlewares;

/// <summary>
/// <see cref="WebApplication"/> для настройки слоя логирования HTTP-запросов.
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    /// <summary>
    /// Настраивает слой логирования HTTP-запросов.
    /// </summary>
    /// <remarks>
    /// <see cref="Serilog.SerilogApplicationBuilderExtensions.UseSerilogRequestLogging(IApplicationBuilder, Action{Serilog.AspNetCore.RequestLoggingOptions}?)"/>.
    /// </remarks>
    public static WebApplication UseCustomRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging((configuration) =>
        {
            // Добавляю кастомное свойство для SerilogRequestLogging, т.к свойства NewLine по умолчанию нет
            // А я хочу сделать читабельный Request Logging
            configuration.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("NewLine", Environment.NewLine);
                diagnosticContext.Set("Protocol", httpContext.Request.Protocol);
            };
            configuration.IncludeQueryInRequestPath = true;
            configuration.MessageTemplate = "{Protocol} {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms{NewLine}";
        });

        return app;
    }
}