namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для интеграции с Телеграмом.
/// </summary>
public interface ITelegramIntegrationManager
{
    /// <summary>
    /// Отправляет указанный код по телефонному номеру через Telegram.
    /// </summary>
    /// <param name="number">Телефонный номер получателя.</param>
    /// <param name="code">Код подтверждения.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see langword="true"/>, если код подтверждения отправился.</returns>
    Task<bool> SendVerificationCodeTelegramAsync(string number, string code, CancellationToken ct = default);

    /// <summary>
    /// Проверяет подключение к Telegram серверу.
    /// </summary>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see langword="true"/>, если удалось подключиться.</returns>
    Task<bool> CheckConnectionAsync(CancellationToken ct = default);
}