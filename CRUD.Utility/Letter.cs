namespace CRUD.Utility;

/// <summary>
/// Письмо для электронной почты.
/// </summary>
public class Letter
{
    /// <summary>
    /// Конструктор для создания полноценного письма.
    /// </summary>
    /// <param name="email">Электронная почта получателя.</param>
    /// <param name="subject">Заголовок письма.</param>
    /// <param name="body">Тело письма.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="email"/> или <paramref name="subject"/> или <paramref name="body"/> <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Если <paramref name="email"/> является whitespace'ом.</exception>
    public Letter(string email, string subject, string body)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(email);
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(body);

        Email = email;
        Subject = subject;
        Body = body;
    }

    /// <summary>
    /// Идентификатор письма.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Электронная почта получателя.
    /// </summary>
    public string Email { get; }

    /// <summary>
    /// Заголовок письма.
    /// </summary>
    public string Subject { get; }

    /// <summary>
    /// Тело письма.
    /// </summary>
    public string Body { get; }
}