using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos;

/// <summary>
/// DTO-модель постраничного списка.
/// </summary>
public sealed record PaginatedListDto<T>
{
    /// <summary>
    /// Коллекция объектов <see cref="T"/>.
    /// </summary>
    [JsonPropertyName("items")]
    public required IEnumerable<T> Items { get; init; }

    /// <summary>
    /// Номер страницы.
    /// </summary>
    [JsonPropertyName("pageIndex")]
    public required int PageIndex { get; init; }

    /// <summary>
    /// Размер страницы.
    /// </summary>
    [JsonPropertyName("pageSize")]
    public required int PageSize { get; init; }

    /// <summary>
    /// Всего страниц.
    /// </summary>
    [JsonPropertyName("totalPages")]
    public required int TotalPages { get; init; }

    /// <summary>
    /// Строка поиска.
    /// </summary>
    [JsonPropertyName("searchString")]
    public required string? SearchString { get; init; }

    /// <summary>
    /// Вариант сортировки.
    /// </summary>
    [JsonPropertyName("sortBy")]
    public required string? SortBy { get; init; }

    /// <summary>
    /// Есть ли предыдущая страница.
    /// </summary>
    [JsonPropertyName("hasPreviousPage")]
    public required bool HasPreviousPage { get; init; }

    /// <summary>
    /// Есть ли следующая страница.
    /// </summary>
    [JsonPropertyName("hasNextPage")]
    public required bool HasNextPage { get; init; }
}