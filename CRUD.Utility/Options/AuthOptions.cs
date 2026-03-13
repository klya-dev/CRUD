using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace CRUD.Utility.Options;

/// <summary>
/// Основные параметры аутентификации/авторизации.
/// </summary>
public class AuthOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "Auth";

    /// <summary>
    /// Издатель токена.
    /// </summary>
    /// <remarks>
    /// Обычно это сервер авторизации.
    /// </remarks>
    public required string Issuer { get; set; }

    /// <summary>
    /// Идентификатор ключа (kid).
    /// </summary>
    /// <remarks>
    /// <para>Вписывается в заголовок JWT-токена и в публичный ключ.</para>
    /// <para>Нужно, чтобы микросервис понял через какой публичный ключ проверять токен.</para>
    /// <para>Также, через kid проверяется актуальность публичного ключа в микросервисе (если нет совпадений, то обновляем сведения публичных ключей).</para>
    /// </remarks>
    public required string KeyId { get; set; }

    /// <summary>
    /// Путь до приватного ключа RSA.
    /// </summary>
    public required string PrivateKeyPath { get; set; }

    /// <summary>
    /// Путь до публичного ключа RSA.
    /// </summary>
    public required string PublicKeyPath { get; set; }

    // Кэширование работает идеально, т.к я использую IOptionsMonitor при изменении файла appsettings.json этот класс пересоздаётся, а значит и кэш обновится

    private RsaSecurityKey? _cachedPrivateKey;
    private RsaSecurityKey? _cachedPublicKey;

    /// <summary>
    /// Возвращает приватный ключ, который находится на <see cref="PrivateKeyPath"/> пути.
    /// </summary>
    /// <remarks>
    /// <para>Приватный ключ импортируется из файла один раз и кэшируется.</para>
    /// <para>Чтобы заново импортировать ключ и заново закэшировать нужно <paramref name="getCached"/> = <see langword="false"/>.</para>
    /// </remarks>
    /// <param name="getCached">Получить ли кэшированное значение.</param>
    /// <returns>Приватный ключ.</returns>
    public RsaSecurityKey GetPrivateKey(bool getCached = true)
    {
        // Возвращаем кэшированное значение
        if (_cachedPrivateKey != null && getCached)
            return _cachedPrivateKey;

        // Кэшируем
        _cachedPrivateKey = new RsaSecurityKey(LoadRsaKey(PrivateKeyPath));
        return _cachedPrivateKey;
    }

    /// <summary>
    /// Возвращает публичный ключ, который находится на <see cref="PublicKeyPath"/> пути.
    /// </summary>
    /// <remarks>
    /// <para>Публичный ключ импортируется из файла один раз и кэшируется.</para>
    /// <para>Чтобы заново импортировать ключ и заново закэшировать нужно <paramref name="getCached"/> = <see langword="false"/>.</para>
    /// </remarks>
    /// <param name="getCached">Получить ли кэшированное значение.</param>
    /// <returns>Публичный ключ.</returns>
    public RsaSecurityKey GetPublicKey(bool getCached = true)
    {
        // Возвращаем кэшированное значение
        if (_cachedPublicKey != null && getCached)
            return _cachedPublicKey;

        // Кэшируем
        _cachedPublicKey = new RsaSecurityKey(LoadRsaKey(PublicKeyPath));
        return _cachedPublicKey;
    }

    /// <summary>
    /// Возвращает <see cref="RSA"/> импортированный из указанного файла.
    /// </summary>
    /// <param name="rsaKeyPath">Путь до файла RSA.</param>
    /// <returns>Импортированный <see cref="RSA"/>.</returns>
    /// <exception cref="FileNotFoundException">Если файл ключа RSA не найден.</exception>
    private static RSA LoadRsaKey(string rsaKeyPath)
    {
        var rsa = RSA.Create();
        if (!File.Exists(rsaKeyPath))
            throw new FileNotFoundException("RSA key file not found", rsaKeyPath);
        var pemContents = File.ReadAllText(rsaKeyPath);
        rsa.ImportFromPem(pemContents.ToCharArray());

        return rsa;
    }
}