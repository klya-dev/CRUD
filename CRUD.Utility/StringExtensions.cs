using System.Text.RegularExpressions;

namespace CRUD.Utility;

/// <summary>
/// Расширения для <see cref="string"/>.
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// Состоит ли строка из символов пробела.
    /// </summary>
    /// <exception cref="ArgumentNullException">Если <paramref name="value"/> <see langword="null"/>.</exception>
    /// <returns><see langword="true"/>, если состоит.</returns>
    public static bool IsWhiteSpace(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        for (int i = 0; i < value.Length; i++)
            if (!char.IsWhiteSpace(value[i]))
                return false;

        return true;
    }

    /// <summary>
    /// Заменяет в строке любые символы пробела на один пробел, и заменяет переносы строк на один перенос.
    /// </summary>
    /// <remarks>
    /// Используемые методы: <see cref="ReplaceExtraSpaces(string)"/>, <see cref="ReplaceExtraNewLines(string)"/>".
    /// </remarks>
    /// <returns>Очищенная строка.</returns>
    public static string ReplaceExtraSpacesAndNewLines(this string value)
    {
        return value.ReplaceExtraSpaces().ReplaceExtraNewLines();
    }

    /// <summary>
    /// Заменяет в строке любые символы пробела на один пробел (без учёта переноса строк).
    /// </summary>
    /// <remarks>
    /// Используемый паттерн: <see cref="AnySpacesWithoutNewLinesRegex"/> - "<c>[^\S\r\n]+</c>".
    /// </remarks>
    /// <returns>Очищенная строка.</returns>
    public static string ReplaceExtraSpaces(this string value)
    {
        return AnySpacesWithoutNewLinesRegex().Replace(value, " ");
    }

    /// <summary>
    /// Заменяет в строке переносы строк (<c>\r\n|\n</c>) на один перенос (<c>\n</c>).
    /// </summary>
    /// <remarks>
    /// Используемый паттерн: <see cref="AnyNewLinesRegex"/> - "<c>[\r\n]+</c>".
    /// </remarks>
    /// <returns>Очищенная строка.</returns>
    public static string ReplaceExtraNewLines(this string value)
    {
        return AnyNewLinesRegex().Replace(value, "\n");
    }

    [GeneratedRegex(@"[^\S\r\n]+")] // Любые символы пробела, без учёта переноса строк (https://stackoverflow.com/questions/3469080/match-whitespace-but-not-newlines)
    private static partial Regex AnySpacesWithoutNewLinesRegex();

    [GeneratedRegex(@"[\r\n]+")] // Любые переносы строк
    private static partial Regex AnyNewLinesRegex();
}