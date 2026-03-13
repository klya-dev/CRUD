namespace CRUD.Models.Domains;

/// <summary>
/// Domain модель пользователя.
/// </summary>
public class User
{
    // Почему я выбрал валидацию Domain Model в отдельном методе, а не в setter'е?
    // Хорошая масштабируемость
    // Разделение ответственности
    // Читаемость (модель не засоряется)
    // Единственный минус, это то, что метод валидации нужно вызывать в ручную
    // И решение этого минуса, досточно простое, просто нужно писать нормальные тесты, тем самым разрабу, так или иначе нужно добавить проверку валидации, чтобы прошли тесты :)

    // Для DTO'шек, прописан Fluent Validation, и для Domain моделей тоже, добавление кастомных проверок очень простое. Шикарно

    /// <summary>
    /// Id пользователя.
    /// </summary>
    /// <remarks>
    /// Генерируется при создании экземпляра, автоматически.
    /// </remarks>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Имя пользователя.
    /// </summary>
    public required string Firstname { get; set; }

    /// <summary>
    /// Username пользователя.
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// Хэшированный пароль пользователя.
    /// </summary>
    public required string HashedPassword { get; set; }

    /// <summary>
    /// Код языка пользователя.
    /// </summary>
    public required string LanguageCode { get; set; }

    /// <summary>
    /// Роль пользователя.
    /// </summary>
    public required string Role { get; set; }

    /// <summary>
    /// Является ли пользователь премиумом.
    /// </summary>
    public required bool IsPremium { get; set; }

    /// <summary>
    /// API-ключ пользователя.
    /// </summary>
    /// <remarks>
    /// Не <see langword="null"/>, если <see cref="IsPremium"/> <see langword="true"/>.
    /// </remarks>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Одноразовый API-ключ пользователя.
    /// </summary>
    /// <remarks>
    /// Не <see langword="null"/>, если <see cref="IsPremium"/> <see langword="true"/>.
    /// </remarks>
    public string? DisposableApiKey { get; set; }

    /// <summary>
    /// Версия данных пользователя, при каждом обновлении данных пользователя, обновляется.
    /// </summary>
    /// <remarks>
    /// Используется для решения конфликтов параллельности.
    /// </remarks>
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// URL-путь аватарки пользователя.
    /// </summary>
    public required string AvatarURL { get; set; }

    /// <summary>
    /// Публикации этого пользователя.
    /// </summary>
    /// <remarks>
    /// Необходимо прогружать.
    /// </remarks>
    public ICollection<Publication>? Publications { get; set; }

    /// <summary>
    /// Заказы этого пользователя.
    /// </summary>
    /// <remarks>
    /// Необходимо прогружать.
    /// </remarks>
    public ICollection<Order>? Orders { get; set; }

    /// <summary>
    /// Электронная почта пользователя.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Подтверждёна ли электронная почта пользователя.
    /// </summary>
    public bool IsEmailConfirm { get; set; } = false;

    /// <summary>
    /// Телефонный номер пользователя.
    /// </summary>
    public required string PhoneNumber { get; set; }

    /// <summary>
    /// Подтверждён ли телефонный номер пользователя.
    /// </summary>
    public bool IsPhoneNumberConfirm { get; set; } = false;

    /// <summary>
    /// Уведомления этого пользователя.
    /// </summary>
    /// <remarks>
    /// Необходимо прогружать.
    /// </remarks>
    public ICollection<Notification>? Notifications { get; set; }

    /// <summary>
    /// Токены обновления пользователя.
    /// </summary>
    /// <remarks>
    /// Необходимо прогружать.
    /// </remarks>
    public ICollection<AuthRefreshToken>? AuthRefreshTokens { get; set; }
}