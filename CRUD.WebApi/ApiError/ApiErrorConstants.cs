namespace CRUD.WebApi.ApiError;

/// <summary>
/// Статический класс, который предоставляет готовые ошибки <see cref="ApiError"/> для клиента.
/// </summary>
/// <remarks>
/// В каждой ошибке указанны заголовок, детали, статус, код, с помощью локализированных констант. Используется в эндпоинтах.
/// </remarks>
public static class ApiErrorConstants
{
    /// <summary>
    /// Пустой уникальный идентификатор (GUID).
    /// </summary>
    public static ApiError EmptyUniqueIdentifier => new(ResourceLocalizerConstants.EmptyUniqueIdentifierTitle, ResourceLocalizerConstants.EmptyUniqueIdentifierDetail, (int)HttpStatusCode.BadRequest, ErrorCodes.EMPTY_UNIQUE_IDENTIFIER);

    /// <summary>
    /// Некорректный запрос.
    /// </summary>
    public static ApiError IncorrectRequest => new(ResourceLocalizerConstants.IncorrectRequestTitle, ResourceLocalizerConstants.IncorrectRequestDetail, (int)HttpStatusCode.BadRequest, ErrorCodes.INCORRECT_REQUEST);

    /// <summary>
    /// Пользователь не найден.
    /// </summary>
    public static ApiError UserNotFound => new(ResourceLocalizerConstants.UserNotFoundTitle, ResourceLocalizerConstants.UserNotFoundDetail, (int)HttpStatusCode.NotFound, ErrorCodes.USER_NOT_FOUND);

    /// <summary>
    /// Автор не найден.
    /// </summary>
    public static ApiError AuthorNotFound => new(ResourceLocalizerConstants.AuthorNotFoundTitle, ResourceLocalizerConstants.AuthorNotFoundDetail, (int)HttpStatusCode.NotFound, ErrorCodes.AUTHOR_NOT_FOUND);

    /// <summary>
    /// Публикация не найдена.
    /// </summary>
    public static ApiError PublicationNotFound => new(ResourceLocalizerConstants.PublicationNotFoundTitle, ResourceLocalizerConstants.PublicationNotFoundDetail, (int)HttpStatusCode.NotFound, ErrorCodes.PUBLICATION_NOT_FOUND);

    /// <summary>
    /// Файл не найден.
    /// </summary>
    public static ApiError FileNotFound => new(ResourceLocalizerConstants.FileNotFoundTitle, ResourceLocalizerConstants.FileNotFoundDetail, (int)HttpStatusCode.NotFound, ErrorCodes.FILE_NOT_FOUND);

    /// <summary>
    /// Заказ не найден.
    /// </summary>
    public static ApiError OrderNotFound => new(ResourceLocalizerConstants.OrderNotFoundTitle, ResourceLocalizerConstants.OrderNotFoundDetail, (int)HttpStatusCode.NotFound, ErrorCodes.ORDER_NOT_FOUND);

    /// <summary>
    /// Продукт не найден.
    /// </summary>
    public static ApiError ProductNotFound => new(ResourceLocalizerConstants.ProductNotFoundTitle, ResourceLocalizerConstants.ProductNotFoundDetail, (int)HttpStatusCode.InternalServerError, ErrorCodes.PRODUCT_NOT_FOUND);

    /// <summary>
    /// Уведомление не найдено.
    /// </summary>
    public static ApiError NotificationNotFound => new(ResourceLocalizerConstants.NotificationNotFoundTitle, ResourceLocalizerConstants.NotificationNotFoundDetail, (int)HttpStatusCode.NotFound, ErrorCodes.NOTIFICATION_NOT_FOUND);

    /// <summary>
    /// Уведомление пользователя не найдено.
    /// </summary>
    public static ApiError UserNotificationNotFound => new(ResourceLocalizerConstants.UserNotificationNotFoundTitle, ResourceLocalizerConstants.UserNotificationNotFoundDetail, (int)HttpStatusCode.NotFound, ErrorCodes.USER_NOTIFICATION_NOT_FOUND);

    /// <summary>
    /// Username уже занят.
    /// </summary>
    public static ApiError UsernameAlreadyTaken => new(ResourceLocalizerConstants.UsernameAlreadyTakenTitle, ResourceLocalizerConstants.UsernameAlreadyTakenDetail, (int)HttpStatusCode.BadRequest, ErrorCodes.USERNAME_ALREADY_TAKEN);

    /// <summary>
    /// Email уже занят.
    /// </summary>
    public static ApiError EmailAlreadyTaken => new(ResourceLocalizerConstants.EmailAlreadyTakenTitle, ResourceLocalizerConstants.EmailAlreadyTakenDetail, (int)HttpStatusCode.BadRequest, ErrorCodes.EMAIL_ALREADY_TAKEN);

    /// <summary>
    /// Номер телефона уже занят.
    /// </summary>
    public static ApiError PhoneNumberAlreadyTaken => new(ResourceLocalizerConstants.PhoneNumberAlreadyTakenTitle, ResourceLocalizerConstants.PhoneNumberAlreadyTakenDetail, (int)HttpStatusCode.BadRequest, ErrorCodes.PHONE_NUMBER_ALREADY_TAKEN);

    /// <summary>
    /// Не обнаружено изменений.
    /// </summary>
    public static ApiError NoChangesDetected => new(ResourceLocalizerConstants.NoChangesDetectedTitle, ResourceLocalizerConstants.NoChangesDetectedDetail, (int)HttpStatusCode.BadRequest, ErrorCodes.NO_CHANGES_DETECTED);

    /// <summary>
    /// Конфликт параллельности.
    /// </summary>
    public static ApiError ConcurrencyConflicts => new(ResourceLocalizerConstants.ConcurrencyConflictsTitle, ResourceLocalizerConstants.ConcurrencyConflictsDetail, (int)HttpStatusCode.Conflict, ErrorCodes.CONCURRENCY_CONFLICTS);

    /// <summary>
    /// Неверный логин или пароль.
    /// </summary>
    public static ApiError InvalidLoginOrPassword => new(ResourceLocalizerConstants.InvalidLoginOrPasswordTitle, ResourceLocalizerConstants.InvalidLoginOrPasswordDetail, (int)HttpStatusCode.Unauthorized, ErrorCodes.INVALID_LOGIN_OR_PASSWORD);

    /// <summary>
    /// Неверный пароль.
    /// </summary>
    public static ApiError InvalidPassword => new(ResourceLocalizerConstants.InvalidPasswordTitle, ResourceLocalizerConstants.InvalidPasswordDetail, (int)HttpStatusCode.BadRequest, ErrorCodes.INVALID_PASSWORD);

    /// <summary>
    /// Сигнатура не совпадает.
    /// </summary>
    public static ApiError DoesNotMatchSignature => new(ResourceLocalizerConstants.DoesNotMatchSignatureTitle, ResourceLocalizerConstants.DoesNotMatchSignatureDetail, (int)HttpStatusCode.BadRequest, ErrorCodes.DOES_NOT_MATCH_SIGNATURE);

    /// <summary>
    /// Пользователь не является автором этой публикации.
    /// </summary>
    public static ApiError UserIsNotAuthorOfThisPublication => new(ResourceLocalizerConstants.UserIsNotAuthorOfThisPublicationTitle, ResourceLocalizerConstants.UserIsNotAuthorOfThisPublicationDetail, (int)HttpStatusCode.Forbidden, ErrorCodes.USER_IS_NOT_AUTHOR_OF_THIS_PUBLICATION);

    /// <summary>
    /// Пользователь уже имеет премиум.
    /// </summary>
    public static ApiError UserAlreadyHasPremium => new(ResourceLocalizerConstants.UserAlreadyHasPremiumTitle, ResourceLocalizerConstants.UserAlreadyHasPremiumDetail, (int)HttpStatusCode.BadRequest, ErrorCodes.USER_ALREADY_HAS_PREMIUM);

    /// <summary>
    /// Пользователь не имеет премиум.
    /// </summary>
    public static ApiError UserDoesNotHavePremium => new(ResourceLocalizerConstants.UserDoesNotHavePremiumTitle, ResourceLocalizerConstants.UserDoesNotHavePremiumDetail, (int)HttpStatusCode.Forbidden, ErrorCodes.USER_DOES_NOT_HAVE_PREMIUM);

    /// <summary>
    /// Пользователь уже подтвердил электронную почту.
    /// </summary>
    public static ApiError UserAlreadyConfirmedEmail => new(ResourceLocalizerConstants.UserAlreadyConfirmedEmailTitle, ResourceLocalizerConstants.UserAlreadyConfirmedEmailDetail, (int)HttpStatusCode.BadRequest, ErrorCodes.USER_ALREADY_CONFIRMED_EMAIL);

    /// <summary>
    /// Пользователь не подтвердил электронную почту.
    /// </summary>
    public static ApiError UserHasNotConfirmedEmail => new(ResourceLocalizerConstants.UserHasNotConfirmedEmailTitle, ResourceLocalizerConstants.UserHasNotConfirmedEmailDetail, (int)HttpStatusCode.Forbidden, ErrorCodes.USER_HAS_NOT_CONFIRMED_EMAIL);

    /// <summary>
    /// Пользователь уже подтвердил номер телефона.
    /// </summary>
    public static ApiError UserAlreadyConfirmedPhoneNumber => new(ResourceLocalizerConstants.UserAlreadyConfirmedPhoneNumberTitle, ResourceLocalizerConstants.UserAlreadyConfirmedPhoneNumberDetail, (int)HttpStatusCode.BadRequest, ErrorCodes.USER_ALREADY_CONFIRMED_PHONE_NUMBER);

    /// <summary>
    /// Пользователь не подтвердил номер телефона.
    /// </summary>
    public static ApiError UserHasNotConfirmedPhoneNumber => new(ResourceLocalizerConstants.UserHasNotConfirmedPhoneNumberTitle, ResourceLocalizerConstants.UserHasNotConfirmedPhoneNumberDetail, (int)HttpStatusCode.Forbidden, ErrorCodes.USER_HAS_NOT_CONFIRMED_PHONE_NUMBER);

    /// <summary>
    /// Неверный API-ключ.
    /// </summary>
    public static ApiError InvalidApiKey => new(ResourceLocalizerConstants.InvalidApiKeyTitle, ResourceLocalizerConstants.InvalidApiKeyDetail, (int)HttpStatusCode.Unauthorized, ErrorCodes.INVALID_API_KEY);

    /// <summary>
    /// Неверный или невалидный токен.
    /// </summary>
    public static ApiError InvalidToken => new(ResourceLocalizerConstants.InvalidTokenTitle, ResourceLocalizerConstants.InvalidTokenDetail, (int)HttpStatusCode.BadRequest, ErrorCodes.INVALID_TOKEN);

    /// <summary>
    /// Неверный или невалидный код.
    /// </summary>
    public static ApiError InvalidCode => new(ResourceLocalizerConstants.InvalidCodeTitle, ResourceLocalizerConstants.InvalidCodeDetail, (int)HttpStatusCode.BadRequest, ErrorCodes.INVALID_CODE);

    /// <summary>
    /// Не удалось получить файл.
    /// </summary>
    public static ApiError FailedToReceiveFile => new(ResourceLocalizerConstants.FailedToReceiveFileTitle, ResourceLocalizerConstants.FailedToReceiveFileDetail, (int)HttpStatusCode.InternalServerError, ErrorCodes.FAILED_TO_RECEIVE_FILE); // Ошибка на стороне сервера

    /// <summary>
    /// Не удалось создать файл.
    /// </summary>
    public static ApiError FailedToCreateFile => new(ResourceLocalizerConstants.FailedToCreateFileTitle, ResourceLocalizerConstants.FailedToCreateFileDetail, (int)HttpStatusCode.InternalServerError, ErrorCodes.FAILED_TO_CREATE_FILE);

    /// <summary>
    /// Достигнут лимит размера файла.
    /// </summary>
    public static ApiError FileSizeLimitExceeded => new(ResourceLocalizerConstants.FileSizeLimitExceededTitle, ResourceLocalizerConstants.FileSizeLimitExceededDetail, (int)HttpStatusCode.RequestEntityTooLarge, ErrorCodes.FILE_SIZE_LIMIT_EXCEEDED);

    /// <summary>
    /// Файл уже существует.
    /// </summary>
    public static ApiError FileAlreadyExists => new(ResourceLocalizerConstants.FileAlreadyExistsTitle, ResourceLocalizerConstants.FileAlreadyExistsDetail, (int)HttpStatusCode.Conflict, ErrorCodes.FILE_ALREADY_EXISTS);

    /// <summary>
    /// Письмо уже отправлено.
    /// </summary>
    public static ApiError LetterAlreadySent => new(ResourceLocalizerConstants.LetterAlreadySentTitle, ResourceLocalizerConstants.LetterAlreadySentDetail, (int)HttpStatusCode.BadRequest, ErrorCodes.LETTER_ALREADY_SENT);

    /// <summary>
    /// Код уже отправлен.
    /// </summary>
    public static ApiError CodeAlreadySent => new(ResourceLocalizerConstants.CodeAlreadySentTitle, ResourceLocalizerConstants.CodeAlreadySentDetail, (int)HttpStatusCode.BadRequest, ErrorCodes.CODE_ALREADY_SENT);

    /// <summary>
    /// Оплата не завершена.
    /// </summary>
    public static ApiError PaymentNotCompleted => new(ResourceLocalizerConstants.PaymentNotCompletedTitle, ResourceLocalizerConstants.PaymentNotCompletedDetail, (int)HttpStatusCode.BadRequest, ErrorCodes.PAYMENT_NOT_COMPLETED);

    /// <summary>
    /// Заказ уже выдан или отменён.
    /// </summary>
    public static ApiError OrderAlreadyIssuedOrCanceled => new(ResourceLocalizerConstants.OrderAlreadyIssuedOrCanceledTitle, ResourceLocalizerConstants.OrderAlreadyIssuedOrCanceledDetail, (int)HttpStatusCode.Conflict, ErrorCodes.ORDER_ALREADY_ISSUED_OR_CANCELED);

    /// <summary>
    /// Не удалось создать платёж.
    /// </summary>
    public static ApiError FailedToCreatePayment => new(ResourceLocalizerConstants.FailedToCreatePaymentTitle, ResourceLocalizerConstants.FailedToCreatePaymentDetail, (int)HttpStatusCode.InternalServerError, ErrorCodes.FAILED_TO_CREATE_PAYMENT);

    /// <summary>
    /// Заказ не может быть выдан.
    /// </summary>
    public static ApiError OrderCannotBeIssued => new(ResourceLocalizerConstants.OrderCannotBeIssuedTitle, ResourceLocalizerConstants.OrderCannotBeIssuedDetail, (int)HttpStatusCode.InternalServerError, ErrorCodes.ORDER_CANNOT_BE_ISSUED);

    /// <summary>
    /// Превышен лимит скорости.
    /// </summary>
    public static ApiError RateLimitExceeded => new(ResourceLocalizerConstants.RateLimitExceededTitle, ResourceLocalizerConstants.RateLimitExceededDetail, (int)HttpStatusCode.TooManyRequests, ErrorCodes.RATE_LIMIT_EXCEEDED);

    /// <summary>
    /// Отправленный файл является пустым.
    /// </summary>
    public static ApiError EmptyFile => new(ResourceLocalizerConstants.EmptyFileTitle, ResourceLocalizerConstants.EmptyFileDetail, (int)HttpStatusCode.BadRequest, ErrorCodes.FILE_IS_EMPTY);

    /// <summary>
    /// Сопоставляет ошибки сервиса к клиенту.
    /// </summary>
    /// <remarks>
    /// Для сопоставления используются константы из <see cref="ErrorMessages"/>, они сопоставляются со статическими свойствами из <see cref="ApiErrorConstants"/> через метод <see cref="string.Contains(string)"/>.
    /// </remarks>
    /// <param name="errorMessageFromService">Сообщение с ошибкой из сервиса.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="errorMessageFromService"/> равен <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если нет ни одного совпадения. Т.е. неизвестное сообщение.</exception>
    /// <returns><see cref="ApiError"/>, содержит в себе поля полностью описывающие ошибку.</returns>
    public static ApiError Match(string errorMessageFromService)
    {
        ArgumentNullException.ThrowIfNull(errorMessageFromService);

        return errorMessageFromService switch
        {
            ErrorMessages.EmptyUniqueIdentifier => EmptyUniqueIdentifier,
            ErrorMessages.UserNotFound => UserNotFound,
            ErrorMessages.AuthorNotFound => AuthorNotFound,
            ErrorMessages.PublicationNotFound => PublicationNotFound,
            ErrorMessages.FileNotFound => FileNotFound,
            ErrorMessages.OrderNotFound => OrderNotFound,
            ErrorMessages.ProductNotFound => ProductNotFound,
            ErrorMessages.NotificationNotFound => NotificationNotFound,
            ErrorMessages.UserNotificationNotFound => UserNotificationNotFound,
            ErrorMessages.UsernameAlreadyTaken => UsernameAlreadyTaken,
            ErrorMessages.EmailAlreadyTaken => EmailAlreadyTaken,
            ErrorMessages.PhoneNumberAlreadyTaken => PhoneNumberAlreadyTaken,
            ErrorMessages.NoChangesDetected => NoChangesDetected,
            ErrorMessages.ConcurrencyConflicts => ConcurrencyConflicts,
            ErrorMessages.InvalidLoginOrPassword => InvalidLoginOrPassword,
            ErrorMessages.InvalidPassword => InvalidPassword,
            ErrorMessages.DoesNotMatchSignature => DoesNotMatchSignature,
            ErrorMessages.UserIsNotAuthorOfThisPublication => UserIsNotAuthorOfThisPublication,
            ErrorMessages.UserAlreadyHasPremium => UserAlreadyHasPremium,
            ErrorMessages.UserDoesNotHavePremium => UserDoesNotHavePremium,
            ErrorMessages.UserAlreadyConfirmedEmail => UserAlreadyConfirmedEmail,
            ErrorMessages.UserHasNotConfirmedEmail => UserHasNotConfirmedEmail,
            ErrorMessages.UserAlreadyConfirmedPhoneNumber => UserAlreadyConfirmedPhoneNumber,
            ErrorMessages.UserHasNotConfirmedPhoneNumber => UserHasNotConfirmedPhoneNumber,
            ErrorMessages.InvalidApiKey => InvalidApiKey,
            ErrorMessages.InvalidToken => InvalidToken,
            ErrorMessages.InvalidCode => InvalidCode,
            ErrorMessages.FailedToReceiveFile => FailedToReceiveFile,
            ErrorMessages.FailedToCreateFile => FailedToCreateFile,
            ErrorMessages.FileSizeLimitExceeded => FileSizeLimitExceeded,
            ErrorMessages.FileAlreadyExists => FileAlreadyExists,
            ErrorMessages.LetterAlreadySent => LetterAlreadySent,
            ErrorMessages.CodeAlreadySent => CodeAlreadySent,
            ErrorMessages.PaymentNotCompleted => PaymentNotCompleted,
            ErrorMessages.OrderAlreadyIssuedOrCanceled => OrderAlreadyIssuedOrCanceled,
            ErrorMessages.FailedToCreatePayment => FailedToCreatePayment,
            ErrorMessages.OrderCannotBeIssued => OrderCannotBeIssued,
            _ => throw new InvalidOperationException("Raw outcome: " + errorMessageFromService)
        };
    }
}