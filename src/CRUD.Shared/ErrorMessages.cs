using FluentValidation.Results;

namespace CRUD.Shared;

/// <summary>
/// Статический класс с константами, содержащие сообщения для ошибок сервиса.
/// </summary>
public static class ErrorMessages
{
    /// <summary>
    /// Пустой уникальный идентификатор (GUID).
    /// </summary>
    public const string EmptyUniqueIdentifier = "Empty unique identifier (GUID)";

    /// <summary>
    /// Неверный логин или пароль.
    /// </summary>
    public const string InvalidLoginOrPassword = "Invalid login or password";

    /// <summary>
    /// Неверный пароль.
    /// </summary>
    public const string InvalidPassword = "Invalid password";

    /// <summary>
    /// Username уже занят.
    /// </summary>
    public const string UsernameAlreadyTaken = "This username is already taken";

    /// <summary>
    /// Email уже занят.
    /// </summary>
    public const string EmailAlreadyTaken = "This email is already taken";

    /// <summary>
    /// Номер телефона уже занят.
    /// </summary>
    public const string PhoneNumberAlreadyTaken = "This phone number is already taken";

    /// <summary>
    /// Пользователь не найден.
    /// </summary>
    public const string UserNotFound = "User not found";

    /// <summary>
    /// Автор не найден.
    /// </summary>
    public const string AuthorNotFound = "Author not found";

    /// <summary>
    /// Ключ не найден.
    /// </summary>
    public const string KeyNotFound = "Key not found";

    /// <summary>
    /// Публикация не найдена.
    /// </summary>
    public const string PublicationNotFound = "Publication not found";

    /// <summary>
    /// Файл не найден.
    /// </summary>
    public const string FileNotFound = "File not found";

    /// <summary>
    /// Заказ не найден.
    /// </summary>
    public const string OrderNotFound = "Order not found";

    /// <summary>
    /// Продукт не найден.
    /// </summary>
    public const string ProductNotFound = "Product not found";

    /// <summary>
    /// Уведомление не найдено.
    /// </summary>
    public const string NotificationNotFound = "Notification not found";

    /// <summary>
    /// Уведомление пользователя не найдено.
    /// </summary>
    public const string UserNotificationNotFound = "User Notification not found";

    /// <summary>
    /// Не обнаружено изменений.
    /// </summary>
    public const string NoChangesDetected = "No changes to apply";

    /// <summary>
    /// Конфликт параллельности.
    /// </summary>
    public const string ConcurrencyConflicts = "Concurrency conflicts";

    /// <summary>
    /// Сигнатура не совпадает.
    /// </summary>
    public const string DoesNotMatchSignature = "File signature does not match";

    /// <summary>
    /// Пользователь не является автором этой публикации.
    /// </summary>
    public const string UserIsNotAuthorOfThisPublication = "The user is not the author of this publication";

    /// <summary>
    /// Пользователь уже имеет премиум.
    /// </summary>
    public const string UserAlreadyHasPremium = "The user already has a premium";

    /// <summary>
    /// Пользователь не имеет премиум.
    /// </summary>
    public const string UserDoesNotHavePremium = "The user does not have a premium";

    /// <summary>
    /// Пользователь уже подтвердил электронную почту.
    /// </summary>
    public const string UserAlreadyConfirmedEmail = "The user has already confirmed the email";

    /// <summary>
    /// Пользователь не подтвердил электронную почту.
    /// </summary>
    public const string UserHasNotConfirmedEmail = "The user has not confirmed the email";

    /// <summary>
    /// Пользователь уже подтвердил номер телефона.
    /// </summary>
    public const string UserAlreadyConfirmedPhoneNumber = "The user has already confirmed the phone number";

    /// <summary>
    /// Пользователь не подтвердил номер телефона.
    /// </summary>
    public const string UserHasNotConfirmedPhoneNumber = "The user has not confirmed the phone number";

    /// <summary>
    /// Неверный API-ключ.
    /// </summary>
    public const string InvalidApiKey = "Invalid API-key";

    /// <summary>
    /// Неверный или невалидный токен.
    /// </summary>
    public const string InvalidToken = "Invalid token";

    /// <summary>
    /// Неверный или невалидный код.
    /// </summary>
    public const string InvalidCode = "Invalid code";

    /// <summary>
    /// Не удалось получить файл.
    /// </summary>
    public const string FailedToReceiveFile = "Failed to receive file";

    /// <summary>
    /// Не удалось создать файл.
    /// </summary>
    public const string FailedToCreateFile = "Failed to create file";

    /// <summary>
    /// Достигнут лимит размера файла.
    /// </summary>
    public const string FileSizeLimitExceeded = "File size limit exceeded";

    /// <summary>
    /// Файл уже существует.
    /// </summary>
    public const string FileAlreadyExists = "The file already exists";

    /// <summary>
    /// Письмо уже отправлено.
    /// </summary>
    public const string LetterAlreadySent = "Letter has already been sent";

    /// <summary>
    /// Код уже отправлен.
    /// </summary>
    public const string CodeAlreadySent = "Code has already been sent";

    /// <summary>
    /// Оплата не завершена.
    /// </summary>
    public const string PaymentNotCompleted = "Payment not completed";

    /// <summary>
    /// Заказ уже выдан или отменён.
    /// </summary>
    public const string OrderAlreadyIssuedOrCanceled = "The order has already been issued or canceled";

    /// <summary>
    /// Не удалось создать платёж.
    /// </summary>
    public const string FailedToCreatePayment = "Failed to create payment";

    /// <summary>
    /// Заказ не может быть выдан.
    /// </summary>
    public const string OrderCannotBeIssued = "The order cannot be issued";

    /// <summary>
    /// Возвращает сообщение об ошибке с указанной невалидной моделью и её ошибками валидации.
    /// </summary>
    /// <param name="modelName">Имя невалидной модели.</param>
    /// <param name="errors">Список ошибок валидации.</param>
    /// <returns>Сообщение об ошибке.</returns>
    public static string ModelIsNotValid(string modelName, List<ValidationFailure> errors)
    {
        var errorMessages = string.Join(", ", errors.Select(e => e.ErrorMessage));

        var result = $"{modelName} is not valid.\nErrors: {errorMessages}";
        return result;
    }

    /// <summary>
    /// Возвращает сообщение об ошибке с указанной невалидной моделью и её ошибками валидации.
    /// </summary>
    /// <param name="modelName">Имя невалидной модели.</param>
    /// <param name="errors">Ошибки через запятую.</param>
    /// <returns>Сообщение об ошибке.</returns>
    public static string ModelIsNotValid(string modelName, string errors)
    {
        var result = $"{modelName} is not valid.\nErrors: {errors}";
        return result;
    }

    /// <summary>
    /// Возвращает сообщение об ошибке с указанной невалидной моделью.
    /// </summary>
    /// <param name="modelName">Имя невалидной модели.</param>
    /// <returns>Сообщение об ошибке.</returns>
    public static string ModelIsNotValid(string modelName) => $"{modelName} is not valid.";
}