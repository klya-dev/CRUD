namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> расширения локализации.
/// </summary>
public static class LocalizationExtensions
{
    /// <summary>
    /// Добавляет и настраивает локализацию.
    /// </summary>
    /// <remarks>
    /// Не забывать вызвать <c><see cref="ApplicationBuilderExtensions.UseRequestLocalization(IApplicationBuilder)"/></c> в конфигурации приложения, чтобы язык сопоставлялся с заголовком "Accept-Language" в запросе.
    /// </remarks>
    public static IServiceCollection AddCustomLocalization(this IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddSingleton<IResourceLocalizer, ResourceLocalizer>();
        services.AddSingleton<IValidatorLocalizer, ValidatorLocalizer>();

        var supportedCultures = new[]
        {
            new CultureInfo("ru"),
            new CultureInfo("en")
        };

        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture("ru");

            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;

            options.RequestCultureProviders =
            [
                new AcceptLanguageHeaderRequestCultureProvider()
            ];
        });

        // Назначаем пользовательский LanguageManager для настройки локализации для валидации
        ValidatorOptions.Global.LanguageManager = new CustomValidationLanguageManager();

        return services;

        // Обязательно app.UseRequestLocalization();
        // Чтобы язык сопоставлялся с заголовком "Accept-Language" в запросе
    }
}