namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для информирования пользователя о получении премиума.
/// </summary>
public interface IPremiumInformator
{
    /// <summary>
    /// Информирует пользователя о получении премиума.
    /// </summary>
    /// <param name="email">Электронная почта получателя.</param>
    /// <param name="languageCode">Код языка получателя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="email"/> или <paramref name="languageCode"/> <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    Task InformateAsync(string email, string languageCode, CancellationToken ct = default);
}