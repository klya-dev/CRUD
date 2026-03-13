using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos;

/// <summary>
/// DTO-модель постраничного списка.
/// </summary>
public class PaginatedListDto<T>
{
    /// <summary>
    /// Коллекция объектов <see cref="T"/>.
    /// </summary>
    [JsonPropertyName("items")]
    public required IEnumerable<T> Items { get; set; }

    /// <summary>
    /// Номер страницы.
    /// </summary>
    [JsonPropertyName("pageIndex")]
    public required int PageIndex { get; set; }

    /// <summary>
    /// Размер страницы.
    /// </summary>
    [JsonPropertyName("pageSize")]
    public required int PageSize { get; set; }

    /// <summary>
    /// Всего страниц.
    /// </summary>
    [JsonPropertyName("totalPages")]
    public required int TotalPages { get; set; }

    /// <summary>
    /// Строка поиска.
    /// </summary>
    [JsonPropertyName("searchString")]
    public required string? SearchString { get; set; }

    /// <summary>
    /// Вариант сортировки.
    /// </summary>
    [JsonPropertyName("sortBy")]
    public required string? SortBy { get; set; }

    /// <summary>
    /// Есть ли предыдущая страница.
    /// </summary>
    [JsonPropertyName("hasPreviousPage")]
    public required bool HasPreviousPage { get; set; }

    /// <summary>
    /// Есть ли следующая страница.
    /// </summary>
    [JsonPropertyName("hasNextPage")]
    public required bool HasNextPage { get; set; }
}