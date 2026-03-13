using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CRUD.Infrastructure.S3;

/// <inheritdoc cref="IS3Manager"/>
public class S3Manager : IS3Manager
{
    private readonly string BucketName;
    private readonly string ServiceURL;

    private readonly AmazonS3Client _client;
    private readonly ILogger<S3Manager> _logger;

    public S3Manager(IOptions<S3Options> options, ILogger<S3Manager> logger)
    {
        BucketName = options.Value.BucketName;
        ServiceURL = options.Value.ServiceURL;

        var accessKey = options.Value.AccessKey;
        var secretKey = options.Value.SecretKey;

        AmazonS3Config config = new AmazonS3Config()
        {
            ServiceURL = ServiceURL,
            //SignatureVersion = "4",
            ForcePathStyle = true,
            //AuthenticationRegion = "ru-central-1",
            //RegionEndpoint = Amazon.RegionEndpoint.EUCentral1,
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED, // Иначе постоянно будет злоебучая ошибка XAmzContentSHA256Mismatch, которая спратяна в самом, ни капли информативном, экземпляре исключения (Amazon.Runtime.Internal.HttpErrorResponseException)
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
        };

        _client = new AmazonS3Client(accessKey, secretKey, config);
        _logger = logger;
    }

    public async Task<ServiceResult<Stream>> GetObjectAsync(string key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        // Создаём запрос
        var request = new GetObjectRequest()
        {
            BucketName = BucketName,
            Key = key
        };

        try
        {
            // Вызов сервиса
            var response = await _client.GetObjectAsync(request, ct); // using не нужен, т.к поток пойдёт дальше

            return ServiceResult<Stream>.Success(response.ResponseStream);
        }
        catch (AmazonS3Exception ex)
        {
            // Файл не найден
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Объект \"{key}\" не найден.", key); // Этот лог скорее относится к варианту, когда аватарка не найдена, я решил не хардкодить проверку на содержание "avatars". Если она не найдена, это действительно странно
                return ServiceResult<Stream>.Fail(ErrorMessages.FileNotFound);
            }

            throw;
        }
    }

    public async Task<ServiceResult> CopyObjectAsync(string sourceKey, string destinationKey, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceKey);
        ArgumentNullException.ThrowIfNull(destinationKey);

        // Создаём запрос
        var copyRequest = new CopyObjectRequest()
        {
            SourceBucket = BucketName,
            SourceKey = sourceKey,
            DestinationBucket = BucketName,
            DestinationKey = destinationKey
        };

        try
        {
            // Вызов сервиса
            await _client.CopyObjectAsync(copyRequest, ct);

            return ServiceResult.Success();
        }
        catch (AmazonS3Exception ex)
        {
            // Объект не найден
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Объект \"{key}\" не найден.", sourceKey);
                return ServiceResult.Fail(ErrorMessages.FileNotFound);
            }

            // Конфликт параллельности
            if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                return ServiceResult.Fail(ErrorMessages.ConcurrencyConflicts);

            throw;
        }
    }

    public async Task<ServiceResult> CreateObjectAsync(Stream stream, string key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(key);

        // Объект уже существует
        if (await IsObjectExistsAsync(key, ct))
            return ServiceResult.Fail(ErrorMessages.FileAlreadyExists);

        // Создаём запрос
        var putRequest = new PutObjectRequest()
        {
            BucketName = BucketName,
            Key = key,
            InputStream = stream,
            Headers = {
                ContentLength = stream.Length // Фиксим исключение: Amazon.S3.AmazonS3Exception: "Could not determine content length".
                // Если, например, получить поток от самого S3, и передать его же обратно, чтобы создать объект (GET object (получаем поток) - PUT object (отправляем этот же поток))
                // https://github.com/aws/aws-sdk-net/issues/3146
                // Немного неверная логика, я в ContentLength HTTP-запроса вписываю длину потока, но тесты успешно проходят +ещё я заметил, что если передать неверную длину, например, "1", запрос всё равно отправится и выполнится успешно
                // Ну, а в целом, длина потока совпадает с длиной контента из GET-запроса (GET.ContentLength = Get.ResponseStream)
            }
        };

        try
        {
            // Вызов сервиса
            await _client.PutObjectAsync(putRequest, ct);

            return ServiceResult.Success();
        }
        catch (AmazonS3Exception ex)
        {
            // Лол, если файл существует S3, тупо пересоздаёт его
            // И не выдаёт исключение

            // Файл уже существует
            if (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
                return ServiceResult.Fail(ErrorMessages.FileAlreadyExists);

            // Конфликт параллельности
            if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                return ServiceResult.Fail(ErrorMessages.ConcurrencyConflicts);

            throw;
        }
    }

    public async Task<ServiceResult> CreateObjectAsync(string key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        // Объект уже существует
        if (await IsObjectExistsAsync(key, ct))
            return ServiceResult.Fail(ErrorMessages.FileAlreadyExists);

        // Создаём запрос
        var putRequest = new PutObjectRequest()
        {
            BucketName = BucketName,
            Key = key
        };

        try
        {
            // Вызов сервиса
            await _client.PutObjectAsync(putRequest, ct);

            return ServiceResult.Success();
        }
        catch (AmazonS3Exception ex)
        {
            // Лол, если файл существует S3, тупо пересоздаёт его
            // И не выдаёт исключение

            // Объект уже существует
            if (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
                return ServiceResult.Fail(ErrorMessages.FileAlreadyExists);

            // Конфликт параллельности
            if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                return ServiceResult.Fail(ErrorMessages.ConcurrencyConflicts);

            throw;
        }
    }

    public async Task<ServiceResult> DeleteObjectAsync(string key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        // Объект не найден
        if (!await IsObjectExistsAsync(key, ct))
        {
            _logger.LogWarning("Объект \"{key}\" не найден.", key); // Этот лог скорее относится к варианту, когда аватарка не найдена
            return ServiceResult.Fail(ErrorMessages.FileNotFound);
        }

        // Создаём запрос
        var deleteRequest = new DeleteObjectRequest()
        {
            BucketName = BucketName,
            Key = key
        };

        try
        {
            // Вызов сервиса
            await _client.DeleteObjectAsync(deleteRequest, ct);

            return ServiceResult.Success();
        }
        catch (AmazonS3Exception ex)
        {
            // Почему-то PreconditionFailed не случается

            // Объект не найден
            if (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
                return ServiceResult.Fail(ErrorMessages.FileNotFound);

            // Конфликт параллельности
            if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                return ServiceResult.Fail(ErrorMessages.ConcurrencyConflicts);

            throw;
        }
    }

    public async Task<bool> IsObjectExistsAsync(string key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        // Создаём запрос
        var request = new ListObjectsRequest
        {
            BucketName = BucketName,
            Prefix = key,
            MaxKeys = 1
        };

        // Вызов сервиса
        var response = await _client.ListObjectsAsync(request, ct);

        // Объект не найден
        if (response.S3Objects == null)
            return false;

        return response.S3Objects.Count != 0;
    }

    public async Task<bool> CheckConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            ListBucketsResponse response = await _client.ListBucketsAsync(ct);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    [Obsolete("Метод не протестирован")]
    public async Task GetListBucketsAsync(CancellationToken ct = default)
    {
        ListBucketsResponse response = await _client.ListBucketsAsync(ct);
        foreach (S3Bucket bucket in response.Buckets)
            Console.WriteLine("{0}\t{1}", bucket.BucketName, bucket.CreationDate);
    }

    [Obsolete("Метод не протестирован")]
    public async Task GetListObjectsAsync(CancellationToken ct = default)
    {
        var request = new ListObjectsRequest()
        {
            BucketName = BucketName,
        };
        ListObjectsResponse responseObjects = await _client.ListObjectsAsync(request, ct);
        foreach (S3Object o in responseObjects.S3Objects)
            Console.WriteLine("{0}\t{1}\t{2}", o.Key, o.Size, o.LastModified);
    }
}