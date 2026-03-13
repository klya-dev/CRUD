namespace CRUD.Models.Validators;

/// <summary>
/// Валидатор строки поиска.
/// </summary>
public class SearchStringValidator
{
    /// <summary>
    /// Максимальная длина поисковой строки.
    /// </summary>
    public const int MAX_LENGTH = 50;

    /// <summary>
    /// Возвращает очищенную строку поиска.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>
    /// <term>Если длина строки больше <see cref="MAX_LENGTH"/></term>
    /// <description>строка обрезается до <see cref="MAX_LENGTH"/> символов.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <param name="searchString">Строка поиска.</param>
    /// <returns>Очищенная строка поиска. Возвращается <see langword="null"/>, если <paramref name="searchString"/> был <see langword="null"/>.</returns>
    public static string? GetSanitizedSearchString(string? searchString)
    {
        // Если длина строки поиска больше MAX_LENGTH, то обрезаем
        if (searchString?.Length > MAX_LENGTH)
            searchString = searchString.Remove(MAX_LENGTH);

        return searchString;
    }
}