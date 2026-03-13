using System.Security.Cryptography;

namespace CRUD.Services;

/// <inheritdoc cref="IUserApiKeyManager"/>
public class UserApiKeyManager : IUserApiKeyManager
{
    /// <summary>
    /// Количество генерируемых безопасных байтов.
    /// </summary>
    private const int NumberOfSecureBytesToGenerate = 100;

    /// <summary>
    /// Длина API-ключа.
    /// </summary>
    private const int ApiKeyLenght = 100;

    // https://www.camiloterevinto.com/post/simple-and-secure-api-keys-using-asp-net-core
    public string GenerateUserApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(NumberOfSecureBytesToGenerate);
        string base64String = Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_");

        return base64String[..ApiKeyLenght]; // Взять первые 100 символов | https://metanit.com/sharp/tutorial/2.32.php
    }

    // Одноразовый ключ
    public string GenerateDisposableUserApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(NumberOfSecureBytesToGenerate);
        string base64String = Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_");

        return base64String[..ApiKeyLenght]; // Взять первые 100 символов | https://metanit.com/sharp/tutorial/2.32.php
    }
}