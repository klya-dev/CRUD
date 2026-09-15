namespace CRUD.Utility;

/// <summary>
/// Статический класс для получения данных о СМС.
/// </summary>
public static class PhoneMessages
{
    /// <summary>
    /// Подтвердить телефонный номер.
    /// </summary>
    public const string VerificatePhoneNumber = "VerificatePhoneNumber";

    /// <summary>
    /// Возвращает локализированные данные для сообщения по предоставленному ключу и коду языка, с поддержкой аргументов.
    /// </summary>
    /// <remarks>
    /// <para>Ключ берётся из констант <see cref="PhoneMessages"/>.</para>
    /// <para>Если предоставленный язык не поддерживается, то используется английский вариант.</para>
    /// 
    /// <para>Аргументы:</para>
    /// <list type="bullet">
    /// <item>
    /// <see cref="VerificatePhoneNumber"/>
    /// <list type="number">
    /// <item>Код.</item>
    /// </list>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="key">Ключ из <see cref="PhoneMessages"/>.</param>
    /// <param name="httpContextAccessor">Нужен для полученния URL приложения.</param>
    /// <param name="languageCode">Код языка.</param>
    /// <param name="args">Аргументы к сообщению. Например код.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="key"/> или <paramref name="languageCode"/> равен <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если нет ни одного совпадения. Т.е. неизвестный ключ (сообщение).</exception>
    /// <returns>Строка, содержащая сообщение.</returns>
    public static string GetMessage(string key, string languageCode, string baseUrl, params string[] args)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(languageCode);

        baseUrl = "CRUD"; // ТЕСТ | СЕРВИС ОТКЛОНЯЕТ СМС ИЗ-ЗА НЕДЕЙСТВУЮЩЕЙ ССЫЛКИ (localhost)

        return (key, languageCode) switch
        {
            (VerificatePhoneNumber, "ru") => $"Код подтверждения: {args[0]}.\nДля сервиса: {baseUrl}",
            (VerificatePhoneNumber, _) => $"Verification code: {args[0]}.\nFor service: {baseUrl}",

            _ => throw new InvalidOperationException("Raw outcome: " + key)
        };
    }
}