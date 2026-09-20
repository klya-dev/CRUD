namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> расширения ApiVersioning.
/// </summary>
public static class ApiVersioningExtensions
{
    /// <summary>
    /// Добавляет и настраивает API версионирование.
    /// </summary>
    public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = false; // Отключаю автоматическое переадресование, если в URL не указали версию | https://stackoverflow.com/questions/52490065/assumedefaultversionwhenunspecified-is-not-working-as-expected
            //options.DefaultApiVersion = new ApiVersion(1, 0); // Нужно для AssumeDefaultVersionWhenUnspecified, например https://localhost:7260/publications?count=2 будет переадресован на https://localhost:7260/v1/publications?count=2
            options.ReportApiVersions = true; // Добавлять ли в заголовок ответа "api-supported-versions", "api-deprecated-versions"
            options.ApiVersionReader = new UrlSegmentApiVersionReader(); // Версию указывается по URL
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'V"; // v2, v2.0, но не v2.0.0 https://localhost:7260/v2.0/publications?count=1 // https://github.com/dotnet/aspnet-api-versioning/wiki/Version-Format#custom-api-version-format-strings
            options.SubstituteApiVersionInUrl = true; // Для подставки в роут-параметры
        });

        return services;
    }
}