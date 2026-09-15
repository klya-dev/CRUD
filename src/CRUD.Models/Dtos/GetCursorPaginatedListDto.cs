using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos;

/// <summary>
/// DTO-модель для получения постраничного списка на основе курсора.
/// </summary>
public sealed record GetCursorPaginatedListDto
{
    /// <summary>
    /// Дата создания.
    /// </summary>
    /// <remarks>
    /// Для сортировки.
    /// </remarks>
    [JsonPropertyName("date")]
    public required DateTime? Date { get; init; }

    /// <summary>
    /// Id курсора.
    /// </summary>
    /// <remarks>
    /// С этого Id начнётся следующий шаг (nextId из ответа).
    /// </remarks>
    [JsonPropertyName("lastId")]
    public required Guid? LastId { get; init; }

    /// <summary>
    /// Количество элементов в шаге (страница).
    /// </summary>
    [JsonPropertyName("limit")]
    public required int Limit { get; init; }
}