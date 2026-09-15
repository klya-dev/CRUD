namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для работы с генерацией, хэшированием и верификацией паролей.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Генерирует хэшированный пароль.
    /// </summary>
    /// <param name="password">Пароль.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="password"/> <see langword="null"/>.</exception>
    /// <returns>Строка, содержащая сгенерированный хэшированный пароль.</returns>
    string GenerateHashedPassword(string password);

    /// <summary>
    /// Проверяет сходство паролей, путём дехэшированния пароля.
    /// </summary>
    /// <param name="password">Пароль.</param>
    /// <param name="hashedPassword">Хэшированный пароль.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="password"/> или <paramref name="hashedPassword"/> <see langword="null"/>.</exception>
    /// <returns><see langword="true"/>, если пароли совпадают.</returns>
    bool Verify(string password, string hashedPassword);
}