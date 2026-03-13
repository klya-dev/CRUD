namespace CRUD.Models.Validators.ValidatorsLocalizer.Languages;

/// <summary>
/// Статический класс для локализации русского языка.
/// </summary>
public static class RussianValidatorsLanguage
{
    /// <summary>
    /// Возвращает локализированное значение для русского языка по ключу из <see cref="ValidatorsLocalizerConstants"/>.
    /// </summary>
    /// <remarks>
    /// Если ключ не найден, то возвращается <see langword="null"/>.
    /// </remarks>
    /// <param name="key">Ключ из <see cref="ValidatorsLocalizerConstants"/>.</param>
    /// <returns>Локализированная строка.</returns>
    public static string GetTranslation(string key) => key switch
    {
        ValidatorsLocalizerConstants.PropertyFirstname => "Имя",
        ValidatorsLocalizerConstants.PropertyLanguageCode => "Код языка",
        ValidatorsLocalizerConstants.PropertyPhoneNumber => "Номер телефона",
        ValidatorsLocalizerConstants.PropertyPassword => "Пароль",
        ValidatorsLocalizerConstants.PropertyHashedPassword => "Хэшированный пароль",
        ValidatorsLocalizerConstants.PropertyNewPassword => "Новый пароль",
        ValidatorsLocalizerConstants.PropertyToken => "Токен",
        ValidatorsLocalizerConstants.PropertyApiKey => "API-ключ",
        ValidatorsLocalizerConstants.PropertyDisposableApiKey => "Одноразовый API-ключ",
        ValidatorsLocalizerConstants.PropertyApiKeyOrDisposableApiKey => "Постоянный или одноразовый API-ключ",
        ValidatorsLocalizerConstants.PropertyTitle => "Заголовок",
        ValidatorsLocalizerConstants.PropertyContent => "Содержимое",
        ValidatorsLocalizerConstants.PropertyCount => "Количество",
        ValidatorsLocalizerConstants.PropertyDate => "Дата",
        ValidatorsLocalizerConstants.PropertyCreatedAt => "Дата создания",
        ValidatorsLocalizerConstants.PropertyEditedAt => "Дата изменения",
        ValidatorsLocalizerConstants.PropertyPageSize => "Размер страницы",
        ValidatorsLocalizerConstants.PropertyPageIndex => "Номер страницы",

        ValidatorsLocalizerConstants.OnlyCyrillic => "'{PropertyName}' должен состоять из Кириллицы.",
        ValidatorsLocalizerConstants.OnlyLatin => "'{PropertyName}' должен состоять из Латиницы.",
        ValidatorsLocalizerConstants.OnlySmallCaseLatin => "'{PropertyName}' должен состоять из нижнего регистра Латиницы.",
        ValidatorsLocalizerConstants.OnlyLatinNumbersDashes => "'{PropertyName}' может состоять только из Латиницы, цифр, нижнего подчёркивания и тире.",
        ValidatorsLocalizerConstants.OnlyLatinNumbersSpecialCharacters => "'{PropertyName}' может состоять только из Латиницы, цифр, специальных символов.",
        ValidatorsLocalizerConstants.OnlyNumbers => "'{PropertyName}' может состоять только из цифр.",
        ValidatorsLocalizerConstants.NotWhiteSpace => "'{PropertyName}' должно быть заполнено.",
        ValidatorsLocalizerConstants.Email => "Неверный Email.",
        ValidatorsLocalizerConstants.InvalidRole => "Неверная роль.",
        ValidatorsLocalizerConstants.InvalidOrderStatus => "Неверный статус заказа.",
        ValidatorsLocalizerConstants.InvalidPaymentStatus => "Неверный статус оплаты.",
        ValidatorsLocalizerConstants.InvalidProductName => "Неверное имя продукта.",
        ValidatorsLocalizerConstants.InvalidDateJson => "Неверная дата ({0}).",

        ValidatorsLocalizerConstants.TestParams => "'{PropertyName}' ИНФОРМИРУЕТ: $A$ больше, чем $B$.",
        _ => null!
    };
}