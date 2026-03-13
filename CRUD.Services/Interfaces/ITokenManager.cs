using System.Security.Claims;

namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для работы с токенами.
/// </summary>
public interface ITokenManager
{
    /// <summary>
    /// Генерирует JWT-токены AccessToken и RefreshToken для аутентификации и авторизации.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="claims"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="username"/> <see langword="null"/> или пустой</term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="username"/> является whitespace'ом</term>
    /// <description>исключение <see cref="ArgumentException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="claims"/> не содержит ни одного элемента</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="claims">Полезная информация, которая будет в JWT-токене.</param>
    /// <param name="username">Username получателя.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="claims"/> или <paramref name="username"/> <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Если <paramref name="username"/> является whitespace'ом.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="claims"/> не содержит ни одного элемента.</exception>
    /// <returns><see cref="AuthJwtResponse"/>.</returns>
    AuthJwtResponse GenerateAuthResponse(IEnumerable<Claim> claims, string username);

    /// <summary>
    /// Генерирует Refresh-токен аутентификации.
    /// </summary>
    /// <returns>Строка, содержащая токен.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Генерирует уникальный токен.
    /// </summary>
    /// <remarks>
    /// Подойдёт для подтверждения через письмо на электронной почте.
    /// </remarks>
    /// <returns>Строка, содержащая токен.</returns>
    string GenerateUniqueToken();

    /// <summary>
    /// Генерирует код из цифр.
    /// </summary>
    /// <param name="length">Длина кода.</param>
    /// <returns>Строка, содержащая код.</returns>
    string GenerateCode(int length = 6);
}