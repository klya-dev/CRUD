using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Text;

namespace CRUD.Utility;

/// <summary>
/// Вспомогательный класс для работы с OAuth.
/// </summary>
public static class OAuthHelper
{
    /// <summary>
    /// Генерирует CodeVerifier.
    /// </summary>
    /// <remarks>
    /// С помощью этого кода можно получить CodeChallenge (<see cref="GetCodeChallenge(string)"/>).
    /// </remarks>
    /// <returns>CodeVerifier закодированный в Base64 через <see cref="WebEncoders.Base64UrlEncode(byte[])"/>.</returns>
    public static string GenerateCodeVerifier()
    {
        using var random = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        random.GetBytes(bytes);

        var codeVerifier = WebEncoders.Base64UrlEncode(bytes);
        return codeVerifier;
    }

    /// <summary>
    /// Возвращает CodeChallenge полученный из CodeVerifier.
    /// </summary>
    /// <remarks>
    /// <para>Для хэширования используется <see cref="SHA256"/>.</para>
    /// <para>Метод соответствует RFC <seealso href="https://datatracker.ietf.org/doc/html/rfc7636#section-4.2"/>.</para>
    /// </remarks>
    /// <param name="codeVerifier">CodeVerifier (<see cref="GenerateCodeVerifier"/>).</param>
    /// <returns>CodeChallenge закодированный в Base64 через <see cref="WebEncoders.Base64UrlEncode(byte[])"/>.</returns>
    public static string GetCodeChallenge(string codeVerifier)
    {
        var challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)); // По RFC используем ASCII (https://datatracker.ietf.org/doc/html/rfc7636#section-4.2)
        return WebEncoders.Base64UrlEncode(challengeBytes);
    }

    /// <summary>
    /// Генерирует Nonce (случайная строка).
    /// </summary>
    /// <param name="byteLength">Длина байт.</param>
    /// <returns>Nonce закодированный в Base64 через <see cref="WebEncoders.Base64UrlEncode(byte[])"/>.</returns>
    public static string GenerateNonce(int byteLength = 32)
    {
        byte[] byteArray = new byte[byteLength];

        using var random = RandomNumberGenerator.Create();
        random.GetBytes(byteArray);

        return WebEncoders.Base64UrlEncode(byteArray);
    }

    /// <summary>
    /// Скачивает изображение по указанному Url.
    /// </summary>
    /// <remarks>
    /// Метод получает поток байт изображения и копирует в созданный <see cref="MemoryStream"/> и возвращает его.
    /// </remarks>
    /// <param name="url">Url изображения.</param>
    /// <returns>Возвращает <see cref="Stream"/> изображения.</returns>
    public static async Task<Stream> DownloadPictureAsync(string url)
    {
        using var httpClient = new HttpClient();

        // Получаем поток байт изображения
        var stream = await httpClient.GetStreamAsync(url);

        // Копируем в MemoryStream
        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        return memoryStream;
    }
}