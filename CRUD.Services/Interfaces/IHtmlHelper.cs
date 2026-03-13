namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для работы с HTML.
/// </summary>
public interface IHtmlHelper
{
    /// <summary>
    /// Очищает Html от недопустимого и вредоностного кода.
    /// </summary>
    /// <param name="html">Html код.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="html"/> <see langword="null"/>.</exception>
    /// <returns>Очищенный Html код.</returns>
    string SanitizeHtml(string html);
}