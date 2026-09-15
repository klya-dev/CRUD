using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace Microservice.EmailSender.Tests.Helpers;

/// <summary>
/// Сервис для выдачи тестовых токенов.
/// </summary>
public static class TokenManager
{
    // Генерируем ключи в памяти, чтобы не тащить настоящие/тестовые из файлов

    /// <summary>
    /// Сгенерированный <see cref="RSA"/>.
    /// </summary>
    public static readonly RSA RsaKey = RSA.Create(2048);

    /// <summary>
    /// Тестовый сгенерированный приватный ключ из <see cref="RsaKey"/>.
    /// </summary>
    public static readonly RsaSecurityKey PrivateKey = new RsaSecurityKey(RsaKey);

    /// <summary>
    /// Тестовый сгенерированный публичный ключ из <see cref="RsaKey"/>.
    /// </summary>
    public static readonly RsaSecurityKey PublicKey = new RsaSecurityKey(RsaKey.ExportParameters(false));

    private static readonly JwtSecurityTokenHandler _tokenHandler = new JwtSecurityTokenHandler();

    /// <summary>
    /// Генерирует токен аутентификации для EmailSender'а.
    /// </summary>
    public static string GenerateEmailSenderAuthToken()
    {
        // Создаем JWT-токен
        var jwt = new JwtSecurityToken(
                issuer: "TestIssuer",
                audience: "TestAudience",
                expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(30)),
                signingCredentials: new SigningCredentials(PrivateKey, SecurityAlgorithms.RsaSha256));
        var encodedJwt = _tokenHandler.WriteToken(jwt);

        return encodedJwt;
    }
}