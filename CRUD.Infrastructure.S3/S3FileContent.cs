namespace CRUD.Infrastructure.S3;

/// <summary>
/// Результат GET-запросов с данными объекта.
/// </summary>
/// <param name="Stream">Поток файла.</param>
/// <param name="ContentType">Тип контента.</param>
/// <param name="ETag">ETag.</param>
/// <param name="ContentLength">Длина контента.</param>
/// <param name="CustomMetadata">Пользовательские метаданные.</param>
public sealed record S3FileContent(
    Stream Stream,
    string ContentType,
    string ETag,
    long ContentLength,
    IDictionary<string, string> CustomMetadata) : IDisposable, IAsyncDisposable
{
    public void Dispose()
    {
        Stream?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await (Stream?.DisposeAsync() ?? ValueTask.CompletedTask);
        GC.SuppressFinalize(this);
    }
}