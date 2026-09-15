using Amazon.Runtime;
using Amazon.S3.Model;

namespace CRUD.Infrastructure.S3;

/// <summary>
/// Статический класс расширений для маппинга ответов S3 в их DTO-результаты.
/// </summary>
public static class S3MapperExtensions
{
    /// <summary>
    /// Возвращает DTO-результат с данными объекта созданный из <see cref="GetObjectResponse"/>.
    /// </summary>
    /// <param name="response">Ответ S3.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="response"/> <see langword="null"/>.</exception>
    /// <returns>DTO-результат с данными объекта.</returns>
    public static S3FileContent ToFileContent(this GetObjectResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new S3FileContent(
            Stream: response.ResponseStream,
            ContentType: response.Headers.ContentType,
            ETag: response.ETag,
            ContentLength: response.ContentLength,
            CustomMetadata: response.Metadata.Keys.ToDictionary(key => key, key => response.Metadata[key])
        );
    }

    /// <summary>
    /// Возвращает DTO-результат операции над объектом созданный из базового <see cref="AmazonWebServiceResponse"/>.
    /// </summary>
    /// <param name="response">Ответ S3.</param>
    /// <param name="etag">ETag.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="response"/> <see langword="null"/>.</exception>
    /// <returns>DTO-результат операции над объектом.</returns>
    public static S3OperationResult ToOperationResult(this AmazonWebServiceResponse response, string? etag = null)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new S3OperationResult(
            ETag: etag,
            StatusCode: response.HttpStatusCode,
            ContentLength: response.ContentLength
        );
    }

    /// <summary>
    /// Возвращает DTO-результат операции над объектом созданный из <see cref="PutObjectResponse"/>.
    /// </summary>
    /// <param name="response">Ответ S3.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="response"/> <see langword="null"/>.</exception>
    /// <returns>DTO-результат операции над объектом.</returns>
    public static S3OperationResult ToOperationResult(this PutObjectResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new S3OperationResult(
            ETag: response.ETag,
            StatusCode: response.HttpStatusCode,
            ContentLength: response.ContentLength,
            VersionId: response.VersionId
        );
    }

    /// <summary>
    /// Возвращает DTO-результат операции над объектом созданный из <see cref="CopyObjectResponse"/>.
    /// </summary>
    /// <param name="response">Ответ S3.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="response"/> <see langword="null"/>.</exception>
    /// <returns>DTO-результат операции над объектом.</returns>
    public static S3OperationResult ToOperationResult(this CopyObjectResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new S3OperationResult(
            ETag: response.ETag,
            StatusCode: response.HttpStatusCode,
            ContentLength: response.ContentLength,
            VersionId: response.VersionId
        );
    }
}