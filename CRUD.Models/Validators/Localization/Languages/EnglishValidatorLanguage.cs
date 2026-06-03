namespace CRUD.Models.Validators.Localization.Languages;

/// <summary>
/// Статический класс для локализации английского языка.
/// </summary>
public static class EnglishValidatorLanguage
{
    /// <summary>
    /// Возвращает локализированное значение для английского языка по ключу из <see cref="ValidatorLocalizerConstants"/>.
    /// </summary>
    /// <remarks>
    /// Если ключ не найден, то возвращается <see langword="null"/>.
    /// </remarks>
    /// <param name="key">Ключ из <see cref="ValidatorLocalizerConstants"/>.</param>
    /// <returns>Локализированная строка.</returns>
    public static string GetTranslation(string key) => key switch
    {
        ValidatorLocalizerConstants.PropertyFirstname => "Firstname",
        ValidatorLocalizerConstants.PropertyLanguageCode => "Language Code",
        ValidatorLocalizerConstants.PropertyPhoneNumber => "Phone Number",
        ValidatorLocalizerConstants.PropertyPassword => "Password",
        ValidatorLocalizerConstants.PropertyHashedPassword => "Hashed password",
        ValidatorLocalizerConstants.PropertyNewPassword => "New password",
        ValidatorLocalizerConstants.PropertyToken => "Token",
        ValidatorLocalizerConstants.PropertyApiKey => "API-key",
        ValidatorLocalizerConstants.PropertyDisposableApiKey => "Disposable API-key",
        ValidatorLocalizerConstants.PropertyApiKeyOrDisposableApiKey => "Permanent or disposable API-key",
        ValidatorLocalizerConstants.PropertyTitle => "Title",
        ValidatorLocalizerConstants.PropertyContent => "Content",
        ValidatorLocalizerConstants.PropertyCount => "Count",
        ValidatorLocalizerConstants.PropertyDate => "Date",
        ValidatorLocalizerConstants.PropertyCreatedAt => "Created at",
        ValidatorLocalizerConstants.PropertyEditedAt => "Edited at",
        ValidatorLocalizerConstants.PropertyPageSize => "Page size",
        ValidatorLocalizerConstants.PropertyPageIndex => "Page index",

        ValidatorLocalizerConstants.OnlyCyrillic => "'{PropertyName}' must be in Cyrillic.",
        ValidatorLocalizerConstants.OnlyLatin => "'{PropertyName}' must be in Latin.",
        ValidatorLocalizerConstants.OnlySmallCaseLatin => "'{PropertyName}' must be lowercase Latin.",
        ValidatorLocalizerConstants.OnlyLatinNumbersDashes => "'{PropertyName}' can only consist of Latin characters, numbers, underdash and dash.",
        ValidatorLocalizerConstants.OnlyLatinNumbersSpecialCharacters => "'{PropertyName}' can only consist of Latin characters, numbers, and special characters.",
        ValidatorLocalizerConstants.OnlyNumbers => "'{PropertyName}' can only consist numbers.",
        ValidatorLocalizerConstants.NotWhiteSpace => "'{PropertyName}' must not be empty.",
        ValidatorLocalizerConstants.Email => "Invalid Email.",
        ValidatorLocalizerConstants.InvalidRole => "Invalid role.",
        ValidatorLocalizerConstants.InvalidOrderStatus => "Invalid order status.",
        ValidatorLocalizerConstants.InvalidPaymentStatus => "Invalid payment status.",
        ValidatorLocalizerConstants.InvalidProductName => "Invalid product name.",
        ValidatorLocalizerConstants.InvalidDateJson => "Invalid date ({0}).",

        ValidatorLocalizerConstants.TestParams => "'{PropertyName}' INFORMATE: $A$ more then $B$.",
        _ => null!
    };
}