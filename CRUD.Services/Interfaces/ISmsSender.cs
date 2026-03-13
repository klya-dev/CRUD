namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для отправки СМС.
/// </summary>
public interface ISmsSender
{
    /// <summary>
    /// Отправляет СМС по указанному телефонному номеру.
    /// </summary>
    /// <param name="phoneNumber">Телефонный номер получателя.</param>
    /// <param name="text">Текстовое сообщение.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see langword="true"/>, если СМС отправилось.</returns>
    Task<bool> SendSmsAsync(string phoneNumber, string text, CancellationToken ct = default);

    /// <summary>
    /// Проверяет авторизацию в сервисе.
    /// </summary>
    /// <remarks>
    /// Использовать для проверки подключения.
    /// </remarks>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see langword="true"/>, если удалось авторизоваться.</returns>
    Task<bool> TestAuthAsync(CancellationToken ct = default);
}