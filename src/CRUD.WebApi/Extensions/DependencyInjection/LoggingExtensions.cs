using Serilog.Events;
using Serilog.Enrichers.Sensitive;

namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="WebApplicationBuilder"/> расширения логирования.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Настраивает логирование.
    /// </summary>
    public static WebApplicationBuilder AddLogging(this WebApplicationBuilder builder, ProgramOptions programOptions)
    {
        // Пропускаем ли логирование
        if (programOptions.SkipLogging)
        {
            builder.Logging.ClearProviders();
            return builder;
        }

        S3Options s3Options = builder.Configuration.GetSection(S3Options.SectionName).Get<S3Options>()!;

        Console.OutputEncoding = System.Text.Encoding.UTF8; // Нормальная кодировка в консоле вместо "<" - "«", и другие мелочи
        builder.Logging.ClearProviders(); // Убираем ConsoleLoggerProvider, DebugLoggerProvider, EventSourceLoggerProvider, EventLogLoggerProvider
        builder.Host.UseSerilog((context, configuration) =>
        {
            configuration.Filter.ByExcluding(logEvent =>
            {
                // Исключаем некоторые эндпоинты из логирования
                if (logEvent.Properties.TryGetValue("RequestPath", out var pathProperty)
                    && logEvent.Level <= LogEventLevel.Information // Логируем Warning или выше
                    && pathProperty is ScalarValue scalarPath
                    && scalarPath.Value is string requestPath)
                {
                    return requestPath == "/metrics" || requestPath == "/healthz"; // Можно через StartWith
                }
                return false;
            });
            configuration.WriteTo.Console(outputTemplate: "[{ApplicationName}] [{Timestamp:dd.MM.yyyy HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}");
            configuration.WriteTo.File(Path.Combine(builder.Environment.ContentRootPath, s3Options.LogsDirectory, "log-.txt"),
                outputTemplate: "[{ApplicationName}] [{SourceContext}] [{Timestamp:dd.MM.yyyy HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: null);
            configuration.ReadFrom.Configuration(context.Configuration);
            configuration.Enrich.WithSensitiveDataMasking(options =>
            {
                options.MaskingOperators.Clear(); // По дефолту тут три оператора
                options.MaskingOperators.Add(new AccessTokenMaskingOperator()); // Добавляем маскировку access_token'а
            });
        });

        return builder;
    }
}