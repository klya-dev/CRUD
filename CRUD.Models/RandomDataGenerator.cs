using System.Text;

namespace CRUD.Models;

/// <summary>
/// Генератор рандомных данных.
/// </summary>
public static class RandomDataGenerator
{
    /// <summary>
    /// Допустимые символы для Username.
    /// </summary>
    private const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";

    /// <summary>
    /// Префик сгенерированного Username.
    /// </summary>
    private const string Prefix = "und-";

    /// <summary>
    /// Генерирует случайный Username.
    /// </summary>
    /// <remarks>
    /// <para>Допустимые символы - <see cref="AllowedChars"/>, префикс - <see cref="Prefix"/>, длина - 32 символа.</para>
    /// <para>Пример: "und-MxdyrdnTAoH28u5HapSiHdL55_45".</para>
    /// </remarks>
    /// <returns>Случайный Username.</returns>
    public static string GenerateRandomUsername()
    {
        int length = 32;

        StringBuilder sb = new StringBuilder(Prefix, length);

        for (int i = 0; i < length - Prefix.Length; i++)
            sb.Append(AllowedChars[Random.Shared.Next(AllowedChars.Length)]);

        return sb.ToString();
    }

    /// <summary>
    /// Генерирует случайный пароль.
    /// </summary>
    /// <remarks>
    /// <para>Длина - 32 символа.</para>
    /// </remarks>
    /// <returns>Случайный пароль.</returns>
    public static string GenerateRandomPassword()
    {
        int length = 32;

        return Guid.NewGuid().ToString().Remove(length);
    }
}