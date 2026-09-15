namespace CRUD.Shared;

/// <summary>
/// Статический класс с константами, содержащие коды для ошибок сервиса.
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// Проблема валидации.
    /// </summary>
    public const string VALIDATION_PROBLEM = "VALIDATION_PROBLEM";

    /// <summary>
    /// Пустой уникальный идентификатор (GUID).
    /// </summary>
    public const string EMPTY_UNIQUE_IDENTIFIER = "EMPTY_UNIQUE_IDENTIFIER";

    /// <summary>
    /// Некорректный запрос.
    /// </summary>
    public const string INCORRECT_REQUEST = "INCORRECT_REQUEST";

    /// <summary>
    /// Неверный логин или пароль.
    /// </summary>
    public const string INVALID_LOGIN_OR_PASSWORD = "INVALID_LOGIN_OR_PASSWORD";

    /// <summary>
    /// Неверный пароль.
    /// </summary>
    public const string INVALID_PASSWORD = "INVALID_PASSWORD";

    /// <summary>
    /// Username уже занят.
    /// </summary>
    public const string USERNAME_ALREADY_TAKEN = "USERNAME_ALREADY_TAKEN";

    /// <summary>
    /// Email уже занят.
    /// </summary>
    public const string EMAIL_ALREADY_TAKEN = "EMAIL_ALREADY_TAKEN";

    /// <summary>
    /// Номер телефона уже занят.
    /// </summary>
    public const string PHONE_NUMBER_ALREADY_TAKEN = "PHONE_NUMBER_ALREADY_TAKEN";

    /// <summary>
    /// Пользователь не найден.
    /// </summary>
    public const string USER_NOT_FOUND = "USER_NOT_FOUND";

    /// <summary>
    /// Автор не найден.
    /// </summary>
    public const string AUTHOR_NOT_FOUND = "AUTHOR_NOT_FOUND";

    /// <summary>
    /// Ключ не найден.
    /// </summary>
    public const string KEY_NOT_FOUND = "KEY_NOT_FOUND";

    /// <summary>
    /// Публикация не найдена.
    /// </summary>
    public const string PUBLICATION_NOT_FOUND = "PUBLICATION_NOT_FOUND";

    /// <summary>
    /// Файл не найден.
    /// </summary>
    public const string FILE_NOT_FOUND = "FILE_NOT_FOUND";

    /// <summary>
    /// Заказ не найден.
    /// </summary>
    public const string ORDER_NOT_FOUND = "ORDER_NOT_FOUND";

    /// <summary>
    /// Продукт не найден.
    /// </summary>
    public const string PRODUCT_NOT_FOUND = "PRODUCT_NOT_FOUND";

    /// <summary>
    /// Уведомление не найдено.
    /// </summary>
    public const string NOTIFICATION_NOT_FOUND = "NOTIFICATION_NOT_FOUND";

    /// <summary>
    /// Уведомление пользователя не найдено.
    /// </summary>
    public const string USER_NOTIFICATION_NOT_FOUND = "USER_NOTIFICATION_NOT_FOUND";

    /// <summary>
    /// Не обнаружено изменений.
    /// </summary>
    public const string NO_CHANGES_DETECTED = "NO_CHANGES_DETECTED";

    /// <summary>
    /// Конфликт параллельности.
    /// </summary>
    public const string CONCURRENCY_CONFLICTS = "CONCURRENCY_CONFLICTS";

    /// <summary>
    /// Сигнатура не совпадает.
    /// </summary>
    public const string DOES_NOT_MATCH_SIGNATURE = "DOES_NOT_MATCH_SIGNATURE";

    /// <summary>
    /// Пользователь не является автором этой публикации.
    /// </summary>
    public const string USER_IS_NOT_AUTHOR_OF_THIS_PUBLICATION = "USER_IS_NOT_AUTHOR_OF_THIS_PUBLICATION";

    /// <summary>
    /// Пользователь уже имеет премиум.
    /// </summary>
    public const string USER_ALREADY_HAS_PREMIUM = "USER_ALREADY_HAS_PREMIUM";

    /// <summary>
    /// Пользователь не имеет премиум.
    /// </summary>
    public const string USER_DOES_NOT_HAVE_PREMIUM = "USER_DOES_NOT_HAVE_PREMIUM";

    /// <summary>
    /// Пользователь уже подтвердил электронную почту.
    /// </summary>
    public const string USER_ALREADY_CONFIRMED_EMAIL = "USER_ALREADY_CONFIRMED_EMAIL";

    /// <summary>
    /// Пользователь не подтвердил электронную почту.
    /// </summary>
    public const string USER_HAS_NOT_CONFIRMED_EMAIL = "USER_HAS_NOT_CONFIRMED_EMAIL";

    /// <summary>
    /// Пользователь уже подтвердил номер телефона.
    /// </summary>
    public const string USER_ALREADY_CONFIRMED_PHONE_NUMBER = "USER_ALREADY_CONFIRMED_PHONE_NUMBER";

    /// <summary>
    /// Пользователь не подтвердил номер телефона.
    /// </summary>
    public const string USER_HAS_NOT_CONFIRMED_PHONE_NUMBER = "USER_HAS_NOT_CONFIRMED_PHONE_NUMBER";

    /// <summary>
    /// Неверный API-ключ.
    /// </summary>
    public const string INVALID_API_KEY = "INVALID_API_KEY";

    /// <summary>
    /// Неверный или невалидный токен.
    /// </summary>
    public const string INVALID_TOKEN = "INVALID_TOKEN";

    /// <summary>
    /// Неверный или невалидный код.
    /// </summary>
    public const string INVALID_CODE = "INVALID_CODE";

    /// <summary>
    /// Не удалось получить файл.
    /// </summary>
    public const string FAILED_TO_RECEIVE_FILE = "FAILED_TO_RECEIVE_FILE";

    /// <summary>
    /// Не удалось создать файл.
    /// </summary>
    public const string FAILED_TO_CREATE_FILE = "FAILED_TO_CREATE_FILE";

    /// <summary>
    /// Достигнут лимит размера файла.
    /// </summary>
    public const string FILE_SIZE_LIMIT_EXCEEDED = "FILE_SIZE_LIMIT_EXCEEDED";

    /// <summary>
    /// Файл уже существует.
    /// </summary>
    public const string FILE_ALREADY_EXISTS = "FILE_ALREADY_EXISTS";

    /// <summary>
    /// Письмо уже отправлено.
    /// </summary>
    public const string LETTER_ALREADY_SENT = "LETTER_ALREADY_SENT";

    /// <summary>
    /// Код уже отправлен.
    /// </summary>
    public const string CODE_ALREADY_SENT = "CODE_ALREADY_SENT";

    /// <summary>
    /// Оплата не завершена.
    /// </summary>
    public const string PAYMENT_NOT_COMPLETED = "PAYMENT_NOT_COMPLETED";

    /// <summary>
    /// Заказ уже выдан или отменён.
    /// </summary>
    public const string ORDER_ALREADY_ISSUED_OR_CANCELED = "ORDER_ALREADY_ISSUED_OR_CANCELED";

    /// <summary>
    /// Не удалось создать платёж.
    /// </summary>
    public const string FAILED_TO_CREATE_PAYMENT = "FAILED_TO_CREATE_PAYMENT";

    /// <summary>
    /// Заказ не может быть выдан.
    /// </summary>
    public const string ORDER_CANNOT_BE_ISSUED = "ORDER_CANNOT_BE_ISSUED";

    /// <summary>
    /// Превышен лимит скорости.
    /// </summary>
    public const string RATE_LIMIT_EXCEEDED = "RATE_LIMIT_EXCEEDED";

    /// <summary>
    /// Отправленный файл является пустым.
    /// </summary>
    public const string FILE_IS_EMPTY = "FILE_IS_EMPTY";
}