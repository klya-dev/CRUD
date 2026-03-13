using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace CRUD.Services;

/// <inheritdoc cref="IPasswordHasher"/>
public class PasswordHasher : IPasswordHasher
{
    // Есть нативный вариант https://github.com/dotnet/AspNetCore/blob/main/src/Identity/Extensions.Core/src/PasswordHasher.cs

    private const int SaltSize = 128;
    private const int HashSize = 256;
    private const int Iterations = 100000;
    private static readonly KeyDerivationPrf Argorithm = KeyDerivationPrf.HMACSHA256;

    public string GenerateHashedPassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        // Генерируем 128-битную соль, используя последовательность криптостойких случайный байт
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize / 8); // Делим на 8, чтобы преобразовать биты в байты

        // Получаем 256-битный подраздел (используем HMACSHA256 со 100 000 итераций)
        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: Argorithm,
            iterationCount: Iterations,
            numBytesRequested: HashSize / 8));

        return $"{hashed}-{Convert.ToBase64String(salt)}";
    }

    public bool Verify(string password, string hashedPassword)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(hashedPassword);

        string[] parts = hashedPassword.Split('-');
        byte[] hash = Convert.FromBase64String(parts[0]);
        byte[] salt = Convert.FromBase64String(parts[1]);

        byte[] inputHash = KeyDerivation.Pbkdf2(password, salt, Argorithm, Iterations, HashSize / 8);

        return CryptographicOperations.FixedTimeEquals(hash, inputHash);
    }
}