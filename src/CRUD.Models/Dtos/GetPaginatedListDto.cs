using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos;

/// <summary>
/// DTO-модель для получения постраничного списка.
/// </summary>
public sealed record GetPaginatedListDto
{
    /// <summary>
    /// Номер страницы.
    /// </summary>
    [JsonPropertyName("pageIndex")]
    public required int PageIndex { get; init; }

    /// <summary>
    /// Размер страницы (количество объектов на странице).
    /// </summary>
    [JsonPropertyName("pageSize")]
    public required int PageSize { get; init; }
}