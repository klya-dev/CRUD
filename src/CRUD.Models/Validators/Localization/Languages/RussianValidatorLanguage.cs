namespace CRUD.Models.Validators.Localization.Languages;

/// <summary>
/// Статический класс для локализации русского языка.
/// </summary>
public static class RussianValidatorLanguage
{
    /// <summary>
    /// Возвращает локализированное значение для русского языка по ключу из <see cref="ValidatorLocalizerConstants"/>.
    /// </summary>
    /// <remarks>
    /// Если ключ не найден, то возвращается <see langword="null"/>.
    /// </remarks>
    /// <param name="key">Ключ из <see cref="ValidatorLocalizerConstants"/>.</param>
    /// <returns>Локализированная строка.</returns>
    public static string GetTranslation(string key) => key switch
    {
        ValidatorLocalizerConstants.PropertyFirstname => "Имя",
        ValidatorLocalizerConstants.PropertyLanguageCode => "Код языка",
        ValidatorLocalizerConstants.PropertyPhoneNumber => "Номер телефона",
        ValidatorLocalizerConstants.PropertyPassword => "Пароль",
        ValidatorLocalizerConstants.PropertyHashedPassword => "Хэшированный пароль",
        ValidatorLocalizerConstants.PropertyNewPassword => "Новый пароль",
        ValidatorLocalizerConstants.PropertyToken => "Токен",
        ValidatorLocalizerConstants.PropertyApiKey => "API-ключ",
        ValidatorLocalizerConstants.PropertyDisposableApiKey => "Одноразовый API-ключ",
        ValidatorLocalizerConstants.PropertyApiKeyOrDisposableApiKey => "Постоянный или одноразовый API-ключ",
        ValidatorLocalizerConstants.PropertyTitle => "Заголовок",
        ValidatorLocalizerConstants.PropertyContent => "Содержимое",
        ValidatorLocalizerConstants.PropertyCount => "Количество",
        ValidatorLocalizerConstants.PropertyDate => "Дата",
        ValidatorLocalizerConstants.PropertyCreatedAt => "Дата создания",
        ValidatorLocalizerConstants.PropertyEditedAt => "Дата изменения",
        ValidatorLocalizerConstants.PropertyPageSize => "Размер страницы",
        ValidatorLocalizerConstants.PropertyPageIndex => "Номер страницы",

        ValidatorLocalizerConstants.OnlyCyrillic => "'{PropertyName}' должен состоять из Кириллицы.",
        ValidatorLocalizerConstants.OnlyLatin => "'{PropertyName}' должен состоять из Латиницы.",
        ValidatorLocalizerConstants.OnlySmallCaseLatin => "'{PropertyName}' должен состоять из нижнего регистра Латиницы.",
        ValidatorLocalizerConstants.OnlyLatinNumbersDashes => "'{PropertyName}' может состоять только из Латиницы, цифр, нижнего подчёркивания и тире.",
        ValidatorLocalizerConstants.OnlyLatinNumbersSpecialCharacters => "'{PropertyName}' может состоять только из Латиницы, цифр, специальных символов.",
        ValidatorLocalizerConstants.OnlyNumbers => "'{PropertyName}' может состоять только из цифр.",
        ValidatorLocalizerConstants.NotWhiteSpace => "'{PropertyName}' должно быть заполнено.",
        ValidatorLocalizerConstants.Email => "Неверный Email.",
        ValidatorLocalizerConstants.InvalidRole => "Неверная роль.",
        ValidatorLocalizerConstants.InvalidOrderStatus => "Неверный статус заказа.",
        ValidatorLocalizerConstants.InvalidPaymentStatus => "Неверный статус оплаты.",
        ValidatorLocalizerConstants.InvalidProductName => "Неверное имя продукта.",
        ValidatorLocalizerConstants.InvalidDateJson => "Неверная дата ({0}).",

        ValidatorLocalizerConstants.TestParams => "'{PropertyName}' ИНФОРМИРУЕТ: $A$ больше, чем $B$.",
        _ => null!
    };
}