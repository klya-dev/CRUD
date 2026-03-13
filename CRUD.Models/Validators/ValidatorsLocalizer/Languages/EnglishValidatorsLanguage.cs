namespace CRUD.Models.Validators.ValidatorsLocalizer.Languages;

/// <summary>
/// Статический класс для локализации английского языка.
/// </summary>
public static class EnglishValidatorsLanguage
{
    /// <summary>
    /// Возвращает локализированное значение для английского языка по ключу из <see cref="ValidatorsLocalizerConstants"/>.
    /// </summary>
    /// <remarks>
    /// Если ключ не найден, то возвращается <see langword="null"/>.
    /// </remarks>
    /// <param name="key">Ключ из <see cref="ValidatorsLocalizerConstants"/>.</param>
    /// <returns>Локализированная строка.</returns>
    public static string GetTranslation(string key) => key switch
    {
        ValidatorsLocalizerConstants.PropertyFirstname => "Firstname",
        ValidatorsLocalizerConstants.PropertyLanguageCode => "Language Code",
        ValidatorsLocalizerConstants.PropertyPhoneNumber => "Phone Number",
        ValidatorsLocalizerConstants.PropertyPassword => "Password",
        ValidatorsLocalizerConstants.PropertyHashedPassword => "Hashed password",
        ValidatorsLocalizerConstants.PropertyNewPassword => "New password",
        ValidatorsLocalizerConstants.PropertyToken => "Token",
        ValidatorsLocalizerConstants.PropertyApiKey => "API-key",
        ValidatorsLocalizerConstants.PropertyDisposableApiKey => "Disposable API-key",
        ValidatorsLocalizerConstants.PropertyApiKeyOrDisposableApiKey => "Permanent or disposable API-key",
        ValidatorsLocalizerConstants.PropertyTitle => "Title",
        ValidatorsLocalizerConstants.PropertyContent => "Content",
        ValidatorsLocalizerConstants.PropertyCount => "Count",
        ValidatorsLocalizerConstants.PropertyDate => "Date",
        ValidatorsLocalizerConstants.PropertyCreatedAt => "Created at",
        ValidatorsLocalizerConstants.PropertyEditedAt => "Edited at",
        ValidatorsLocalizerConstants.PropertyPageSize => "Page size",
        ValidatorsLocalizerConstants.PropertyPageIndex => "Page index",

        ValidatorsLocalizerConstants.OnlyCyrillic => "'{PropertyName}' must be in Cyrillic.",
        ValidatorsLocalizerConstants.OnlyLatin => "'{PropertyName}' must be in Latin.",
        ValidatorsLocalizerConstants.OnlySmallCaseLatin => "'{PropertyName}' must be lowercase Latin.",
        ValidatorsLocalizerConstants.OnlyLatinNumbersDashes => "'{PropertyName}' can only consist of Latin characters, numbers, underdash and dash.",
        ValidatorsLocalizerConstants.OnlyLatinNumbersSpecialCharacters => "'{PropertyName}' can only consist of Latin characters, numbers, and special characters.",
        ValidatorsLocalizerConstants.OnlyNumbers => "'{PropertyName}' can only consist numbers.",
        ValidatorsLocalizerConstants.NotWhiteSpace => "'{PropertyName}' must not be empty.",
        ValidatorsLocalizerConstants.Email => "Invalid Email.",
        ValidatorsLocalizerConstants.InvalidRole => "Invalid role.",
        ValidatorsLocalizerConstants.InvalidOrderStatus => "Invalid order status.",
        ValidatorsLocalizerConstants.InvalidPaymentStatus => "Invalid payment status.",
        ValidatorsLocalizerConstants.InvalidProductName => "Invalid product name.",
        ValidatorsLocalizerConstants.InvalidDateJson => "Invalid date ({0}).",

        ValidatorsLocalizerConstants.TestParams => "'{PropertyName}' INFORMATE: $A$ more then $B$.",
        _ => null!
    };
}