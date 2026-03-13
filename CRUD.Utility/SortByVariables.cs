namespace CRUD.Utility;

/// <summary>
/// Статический класс с вариантами сортировки.
/// </summary>
public static class SortByVariables
{
    /// <summary>
    /// Cортировка по дате.
    /// </summary>
    public const string date = "date";

    /// <summary>
    /// Обратная сортировка по дате.
    /// </summary>
    public const string date_desc = "date_desc";

    /// <summary>
    /// Сортировка по количеству публикаций автора.
    /// </summary>
    public const string author_publications_count = "publications_count";

    /// <summary>
    /// Обратная сортировка по количеству публикаций автора.
    /// </summary>
    public const string author_publications_count_desc = "publications_count_desc";
}