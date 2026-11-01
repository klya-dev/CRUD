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
        services.AddSingleton<IResourceLocalizer, ResourceLocalizer>(); // Локализация ответов API на основе ресурсов (PublicationNotFound, NoChangesDetected) 
        services.AddSingleton<IValidatorLocalizer, ValidatorLocalizer>(); // Локализация полей/правил валидаторов ("Постоянный или одноразовый API-ключ" - "Permanent or disposable API-key"); ("Неверный статус оплаты." - "Invalid payment status.")

        var supportedCultures = new[]
        {
            new CultureInfo("ru"),
            new CultureInfo("en")
        };

        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture("ru");

            options.SupportedCultures = supportedCultures; // Форматы дат, чисел, валют
            options.SupportedUICultures = supportedCultures; // Текстовые ресурсы, например: "ru" значит файл ресурсов "ru.resx"

            options.RequestCultureProviders =
            [
                new AcceptLanguageHeaderRequestCultureProvider() // Принимаем язык только из заголовка. Из дефолтых провайдеров, есть ещё query и cookie
            ];
        });

        // Назначаем пользовательский LanguageManager для настройки локализации для валидации
        ValidatorOptions.Global.LanguageManager = new CustomValidationLanguageManager();

        return services;

        // Обязательно app.UseRequestLocalization();
        // Чтобы язык сопоставлялся с заголовком "Accept-Language" в запросе
    }
}