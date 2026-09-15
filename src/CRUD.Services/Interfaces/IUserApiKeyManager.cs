namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для работы с API-ключами пользователя.
/// </summary>
public interface IUserApiKeyManager
{
    /// <summary>
    /// Генерирует API-ключ.
    /// </summary>
    /// <returns>Строка, содержащая в себе сгенерированный API-ключ.</returns>
    string GenerateUserApiKey();

    /// <summary>
    /// Генерирует одноразовый API-ключ.
    /// </summary>
    /// <returns>Строка, содержащая в себе сгенерированный, одноразовый API-ключ.</returns>
    string GenerateDisposableUserApiKey();
}