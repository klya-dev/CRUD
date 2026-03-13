using CRUD.Models.Validators.ValidatorsLocalizer;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

namespace CRUD.WebApi.Extensions;

public static class LocalizationServiceCollectionExtensions
{
    /// <summary>
    /// Добавляет уже настроенную локализацию.
    /// </summary>
    /// <remarks>
    /// Не забывать вызвать <c><see cref="ApplicationBuilderExtensions.UseRequestLocalization(IApplicationBuilder)"/></c> в конфигурации приложения, чтобы язык сопоставлялся с заголовком "Accept-Language" в запросе.
    /// </remarks>
    public static IServiceCollection AddReadyLocalization(this IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddScoped<IResourceLocalizer, ResourceLocalizer.ResourceLocalizer>();
        services.AddScoped<IValidatorsLocalizer, Models.Validators.ValidatorsLocalizer.ValidatorsLocalizer>();

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

        // Не забывать app.UseRequestLocalization();
        // Чтобы язык сопоставлялся с заголовком "Accept-Language" в запросе
    }
}