using Microsoft.EntityFrameworkCore;

namespace CRUD.Models.Dtos;

/// <summary>
/// Постраничный список.
/// </summary>
public class PaginatedList<T> : List<T>
{
    /// <summary>
    /// Номер страницы.
    /// </summary>
    public int PageIndex { get; private set; }

    /// <summary>
    /// Размер страницы.
    /// </summary>
    public int PageSize { get; private set; }

    /// <summary>
    /// Всего страниц.
    /// </summary>
    public int TotalPages { get; private set; }

    /// <summary>
    /// Строка поиска.
    /// </summary>
    public string? SearchString { get; private set; } = null;

    /// <summary>
    /// Вариант сортировки.
    /// </summary>
    public string? SortBy { get; private set; } = null;

    /// <summary>
    /// Создаёт постраничный список из указанного списка объектов, количества объектов из источника, номера и размера страницы.
    /// </summary>
    /// <param name="items">Список из объектов.</param>
    /// <param name="count">Количество объектов из источника.</param>
    /// <param name="pageIndex">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="searchString">Строка поиска.</param>
    /// <param name="sortBy">Вариант сортировки.</param>
    public PaginatedList(List<T> items, int count, int pageIndex, int pageSize, string? searchString = null, string? sortBy = null)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        SearchString = searchString;
        SortBy = sortBy;

        this.AddRange(items);
    }

    /// <summary>
    /// Есть ли предыдущая страница.
    /// </summary>
    public bool HasPreviousPage => PageIndex > 1;

    /// <summary>
    /// Есть ли следующая страница.
    /// </summary>
    public bool HasNextPage => PageIndex < TotalPages;

    /// <summary>
    /// Создаёт постраничный список из источника, номера и размера страницы.
    /// </summary>
    /// <param name="source">Источник объектов.</param>
    /// <param name="pageIndex">Номер страницы.</param>
    /// <param name="pageSize">Количество объектов на странице (размер страницы).</param>
    /// <param name="searchString">Строка поиска.</param>
    /// <param name="sortBy">Вариант сортировки.</param>
    /// <returns>Постраничный список из объектов <see cref="T"/>.</returns>
    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize, string? searchString = null, string? sortBy = null, CancellationToken ct = default)
    {
        var count = await source.CountAsync(ct);
        var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PaginatedList<T>(items, count, pageIndex, pageSize, searchString, sortBy);
    }

    /// <summary>
    /// Создаёт пустой постраничный список из номера и размера страницы (0 элементов).
    /// </summary>
    /// <param name="pageIndex">Номер страницы.</param>
    /// <param name="pageSize">Количество объектов на странице (размер страницы).</param>
    /// <param name="searchString">Строка поиска.</param>
    /// <param name="sortBy">Вариант сортировки.</param>
    /// <returns>Пустой постраничный список из объектов <see cref="T"/>.</returns>
    public static PaginatedList<T> Empty(int pageIndex, int pageSize, string? searchString = null, string? sortBy = null)
    {
        return new PaginatedList<T>([], 0, pageIndex, pageSize, searchString, sortBy);
    }
}