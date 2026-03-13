using System.Globalization;

namespace CRUD.Models.Validators.ValidatorsLocalizer;

/// <summary>
/// Пользовательский <c>LanguageManager</c> для FluentValidation.
/// </summary>
/// <remarks>
/// Указан в <see cref="WebApi.Extensions.LocalizationServiceCollectionExtensions.AddReadyLocalization(IServiceCollection)"/>.
/// </remarks>
public class CustomValidationLanguageManager : FluentValidation.Resources.LanguageManager
{
    public CustomValidationLanguageManager()
    {
        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.CurrentUICulture;

        // Заменяем сообщения по умолчанию (валидатор можно узнать, если перейти в исходный код правила. А перевод, если перейти в AddTranslation и там найти языки)
        AddTranslation("ru", "LengthValidator", "'{PropertyName}' должно быть длиной от {MinLength} до {MaxLength} символов.");
        AddTranslation("en", "LengthValidator", "'{PropertyName}' must be between {MinLength} and {MaxLength} characters.");

        AddTranslation("ru", "ExactLengthValidator", "'{PropertyName}' должно быть длиной {MaxLength} символа(ов).");
        AddTranslation("en", "ExactLengthValidator", "'{PropertyName}' must be {MaxLength} characters in length.");
    }
}