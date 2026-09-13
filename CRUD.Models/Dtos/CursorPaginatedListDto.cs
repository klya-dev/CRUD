using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos;

/// <summary>
/// DTO-модель постраничного списка на основе курсора.
/// </summary>
public sealed record CursorPaginatedListDto<T>
{
    /// <summary>
    /// Коллекция объектов <see cref="T"/>.
    /// </summary>
    [JsonPropertyName("items")]
    public required IEnumerable<T> Items { get; init; }

    /// <summary>
    /// Следущий курсор.
    /// </summary>
    /// <remarks>
    /// Клиент его указывает для следующей итерации. Этот элемент включительно будет в <see cref="Items"/>.
    /// </remarks>
    [JsonPropertyName("nextId")]
    public required Guid? NextId { get; init; }

    /// <summary>
    /// Следущая дата.
    /// </summary>
    /// <remarks>
    /// Клиент его указывает для следующей итерации. Этот элемент включительно будет в <see cref="Items"/>.
    /// </remarks>
    [JsonPropertyName("nextDate")]
    public required DateTime? NextDate { get; init; }

    /// <summary>
    /// Размер страницы (количество элементов).
    /// </summary>
    [JsonPropertyName("limit")]
    public required int Limit { get; init; }

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
    /// Есть ли следующая страница (следующие элементы).
    /// </summary>
    [JsonPropertyName("hasMore")]
    public required bool HasMore { get; init; }

    /// <summary>
    /// Создаёт пустой <see cref="CursorPaginatedListDto{T}"/>.
    /// </summary>
    /// <param name="limit">Количество запрошенных элементов.</param>
    /// <param name="searchString">Строка поиска.</param>
    /// <param name="sortBy">Вариант сортировки.</param>
    public static CursorPaginatedListDto<T> Empty(int limit, string? searchString, string? sortBy)
    {
        return new CursorPaginatedListDto<T>
        {
            Items = [],
            NextId = null,
            NextDate = null,
            Limit = limit,
            SearchString = searchString,
            SortBy = sortBy,
            HasMore = false
        };
    }
}