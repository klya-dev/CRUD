namespace CRUD.Utility;

/// <summary>
/// Статический класс с вариантами сортировки.
/// </summary>
public static class SortByVariables
{
    /// <summary>
    /// Cортировка по дате.
    /// </summary>
    /// <remarks>
    /// От старой к новой.
    /// </remarks>
    public const string date = "date";

    /// <summary>
    /// Обратная сортировка по дате.
    /// </summary>
    /// <remarks>
    /// От новой к старой.
    /// </remarks>
    public const string date_desc = "date_desc";

    /// <summary>
    /// Сортировка по количеству публикаций автора.
    /// </summary>
    /// <remarks>
    /// От меньшего к большему.
    /// </remarks>
    public const string author_publications_count = "publications_count";

    /// <summary>
    /// Обратная сортировка по количеству публикаций автора.
    /// </summary>
    /// <remarks>
    /// От большего к меньшему.
    /// </remarks>
    public const string author_publications_count_desc = "publications_count_desc";
}