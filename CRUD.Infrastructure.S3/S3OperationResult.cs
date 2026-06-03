using System.Net;

namespace CRUD.Infrastructure.S3;

/// <summary>
/// Результат PUT, COPY, DELETE-запросов (операции) над объектом.
/// </summary>
/// <param name="ETag">ETag.</param>
/// <param name="StatusCode">Статус код.</param>
/// <param name="ContentLength">Длина контента.</param>
/// <param name="VersionId">Идентификатор версии.</param>
public sealed record S3OperationResult(
    string? ETag,
    HttpStatusCode StatusCode,
    long ContentLength,
    string? VersionId = null
);