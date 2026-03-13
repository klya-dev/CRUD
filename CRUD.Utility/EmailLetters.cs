namespace CRUD.Utility;

/// <summary>
/// Статический класс для получения данных о письмах.
/// </summary>
public static class EmailLetters
{
    /// <summary>
    /// Подтвердить электронную почту.
    /// </summary>
    public const string EmailConfirm = "EmailConfirm";

    /// <summary>
    /// Подтвердить смену пароля.
    /// </summary>
    public const string ChangePasswordRequest = "ChangePasswordRequest";

    /// <summary>
    /// Информирование пользователя о получении премиума.
    /// </summary>
    public const string InformGettingPremium = "InformGettingPremium";

    /// <summary>
    /// Возвращает локализированные данные для письма по предоставленному ключу и коду языка, с поддержкой аргументов.
    /// </summary>
    /// <remarks>
    /// <para>Ключ берётся из констант <see cref="EmailLetters"/>.</para>
    /// <para>Если предоставленный язык не поддерживается, то используется английский вариант.</para>
    /// 
    /// <para>Аргументы:</para>
    /// <list type="bullet">
    /// <item>
    /// <see cref="EmailConfirm"/>
    /// <list type="number">
    /// <item>Токен.</item>
    /// </list>
    /// </item>
    /// <item>
    /// <see cref="ChangePasswordRequest"/>
    /// <list type="number">
    /// <item>Токен.</item>
    /// </list>
    /// </item>
    /// <item>
    /// <see cref="InformGettingPremium"/>
    /// <list type="number">
    /// <item></item>
    /// </list>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="key">Ключ из <see cref="EmailLetters"/>.</param>
    /// <param name="email">Электронная почта получателя.</param>
    /// <param name="languageCode">Код языка.</param>
    /// <param name="httpContextAccessor">Нужен для полученния URL приложения.</param>
    /// <param name="args">Аргументы к письму. Например токен.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="key"/> или <paramref name="languageCode"/> равен <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если нет ни одного совпадения. Т.е. неизвестный ключ (письмо).</exception>
    /// <returns><see cref="Letter"/> письмо.</returns>
    public static Letter GetLetter(string key, string email, string languageCode, string baseUrl, params string[] args)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(languageCode);

        var serviceName = "CRUD";
        Guid idempotencyKey = Guid.NewGuid();

        return (key, languageCode) switch
        {
            (EmailConfirm, "ru") => new Letter(email, "Подтвердите адрес электронной почты", 
                CreateHtmlBody("Подтвердите адрес электронной почты", $"Вашу почту необходимо подтвердить для сервиса \"{serviceName}\"", "Подтвердить", baseUrl + string.Format(EndpointUrls.ConfirmationsEmailByToken, args[0], idempotencyKey))),
            (EmailConfirm, _) => new Letter(email, "Confirm Email", 
                CreateHtmlBody("Confirm Email", $"Your email needs to be provided for the \"{serviceName}\" service", "Confirm", baseUrl + string.Format(EndpointUrls.ConfirmationsEmailByToken, args[0], idempotencyKey))),

            (ChangePasswordRequest, "ru") => new Letter(email, "Подтвердите смену пароля",
                CreateHtmlBody("Подтвердите смену пароля", $"Вам необходимо подтвердить смену пароля для сервиса \"{serviceName}\"", "Подтвердить", baseUrl + string.Format(EndpointUrls.ConfirmationsPasswordByToken, args[0], idempotencyKey))),
            (ChangePasswordRequest, _) => new Letter(email, "Confirm change password",
                CreateHtmlBody("Confirm change password", $"You need to confirm the password change for the \"{serviceName}\" service", "Confirm", baseUrl + string.Format(EndpointUrls.ConfirmationsPasswordByToken, args[0], idempotencyKey))),

            (InformGettingPremium, "ru") => new Letter(email, "Получение премиума",
                CreateHtmlBody("Получение премиума", $"Поздравляем вас с получением премиума на сервисе \"{serviceName}\"")),
            (InformGettingPremium, _) => new Letter(email, "Getting a premium",
                CreateHtmlBody("Getting a premium", $"Congratulations on receiving premium on the \"{serviceName}\" service")),

            _ => throw new InvalidOperationException("Raw outcome: " + key)
        };
    }

    /// <summary>
    /// Создаёт оформленное тело письма с помощью HTML.
    /// </summary>
    /// <remarks>
    /// Чтобы отобразить кнопку нужно, чтобы <paramref name="nameButton"/> и <paramref name="linkButton"/> не были <see langword="null"/>
    /// </remarks>
    /// <param name="header">Заголовок в теле письма.</param>
    /// <param name="body">Тело (содержимое) в теле письма.</param>
    /// <param name="nameButton">Название кнопки.</param>
    /// <param name="linkButton">Ссылка кнопки (<c>href</c>).</param>
    /// <returns>Оформленное тело письма из HTML.</returns>
    private static string CreateHtmlBody(string header, string body, string? nameButton = null, string? linkButton = null)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(body);

        string h2HeaderStyles = "color: #4C9BE8";
        string trBodyStyles = "padding-bottom: 10px; display: table;";
        string tdButtonStyles = "background-color: #007bff; border-radius: 5px;";
        string aButtonStyles = "font-size: 16px; font-family: Arial, sans-serif; color: #ffffff; text-decoration: none; padding: 10px 20px; display: block;";

        // Вариант с кнопкой
        if (linkButton != null && nameButton != null)
            return $"<table cellspacing='0' cellpadding='0'> <tr> <td> <h2 style='{h2HeaderStyles}'>{header}</h2> </td> </tr> <tr style='{trBodyStyles}'> <td> <span>{body}</span> </td> </tr> <tr> <td align='center' style='{tdButtonStyles}'> <a href='{linkButton}' style='{aButtonStyles}'>{nameButton}</a> </td> </tr> </table>";

        // Вариант без кнопки
        return $"<table cellspacing='0' cellpadding='0'> <tr> <td> <h2 style='{h2HeaderStyles}'>{header}</h2> </td> </tr> <tr style={trBodyStyles}'> <td> <span>{body}</span> </td> </tr> </table>";
    }
}