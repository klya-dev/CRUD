using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace CRUD.Infrastructure.S3;

/// <inheritdoc cref="IS3Manager"/>
public sealed class S3Manager : IS3Manager
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

    public async Task<ServiceResult<S3FileContent>> GetObjectAsync(string key, Action<GetObjectRequest>? options = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        // Создаём запрос
        var request = new GetObjectRequest()
        {
            BucketName = BucketName,
            Key = key
        };

        options?.Invoke(request);

        try
        {
            // Вызов сервиса
            var response = await _client.GetObjectAsync(request, ct); // using не нужен, т.к поток пойдёт дальше

            return ServiceResult<S3FileContent>.Success(response.ToFileContent());
        }
        catch (AmazonS3Exception ex)
        {
            // Объект не найден
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Объект \"{key}\" не найден.", key); // Этот лог скорее относится к варианту, когда аватарка не найдена, я решил не хардкодить проверку на содержание "avatars". Если она не найдена, это действительно странно
                return ServiceResult<S3FileContent>.Fail(ErrorMessages.FileNotFound);
            }

            throw;
        }
    }

    public async Task<ServiceResult<string>> GetPresignedUrlAsync(string key, DateTime? expires = null, Action<GetPreSignedUrlRequest>? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        // Создаём запрос
        var request = new GetPreSignedUrlRequest()
        {
            BucketName = BucketName,
            Key = key,
            Expires = expires ?? DateTime.UtcNow.AddHours(1) // По умолчанию час
        };

        options?.Invoke(request);

        // Вызов сервиса
        var response = await _client.GetPreSignedURLAsync(request);

        return ServiceResult<string>.Success(response);
    }

    public async Task<ServiceResult<S3OperationResult>> CopyObjectAsync(string sourceKey, string destinationKey, Action<CopyObjectRequest>? options = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceKey);
        ArgumentException.ThrowIfNullOrEmpty(destinationKey);

        // Создаём запрос
        var copyRequest = new CopyObjectRequest()
        {
            SourceBucket = BucketName,
            SourceKey = sourceKey,
            DestinationBucket = BucketName,
            DestinationKey = destinationKey
        };

        options?.Invoke(copyRequest);

        try
        {
            // Вызов сервиса
            var response = await _client.CopyObjectAsync(copyRequest, ct);

            return ServiceResult<S3OperationResult>.Success(response.ToOperationResult());
        }
        catch (AmazonS3Exception ex)
        {
            // Объект не найден
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Объект \"{key}\" не найден.", sourceKey);
                return ServiceResult<S3OperationResult>.Fail(ErrorMessages.FileNotFound);
            }

            // Конфликт параллельности
            if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                return ServiceResult<S3OperationResult>.Fail(ErrorMessages.ConcurrencyConflicts);

            throw;
        }
    }

    public async Task<ServiceResult<S3OperationResult>> CreateObjectAsync(string key, Stream? stream = null, Action<PutObjectRequest>? options = null, bool checkExists = true, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        // Создаём запрос
        var putRequest = new PutObjectRequest()
        {
            BucketName = BucketName,
            Key = key,
            InputStream = stream,
            Headers = {
                ContentLength = stream?.Length ?? 0 // Фиксим исключение: Amazon.S3.AmazonS3Exception: "Could not determine content length".
                // Если, например, получить поток от самого S3, и передать его же обратно, чтобы создать объект (GET object (получаем поток) - PUT object (отправляем этот же поток))
                // https://github.com/aws/aws-sdk-net/issues/3146
                // Немного неверная логика, я в ContentLength HTTP-запроса вписываю длину потока, но тесты успешно проходят +ещё я заметил, что если передать неверную длину, например, "1", запрос всё равно отправится и выполнится успешно
                // Ну, а в целом, длина потока совпадает с длиной контента из GET-запроса (GET.ContentLength = Get.ResponseStream)
            }
        };

        options?.Invoke(putRequest);

        // Проверить ли существование объекта
        // Если не проверять, то S3 просто перезапишет объект
        if (checkExists)
        {
            // Вместо отправки ещё одного HTTP-запроса на сервер (IsObjectExistsAsync), добавим заголовок
            putRequest.Headers["If-None-Match"] = "*"; // Выполнить запрос, если объекта с таким ключом не существует, если существует, то исключение со статусом PreconditionFailed
        }

        try
        {
            // Вызов сервиса
            var response = await _client.PutObjectAsync(putRequest, ct);

            return ServiceResult<S3OperationResult>.Success(response.ToOperationResult());
        }
        catch (AmazonS3Exception ex)
        {
            // Объект уже существует (только, если указан заголовок "If-None-Match": "*")
            if (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
                return ServiceResult<S3OperationResult>.Fail(ErrorMessages.FileAlreadyExists);

            // Конфликт параллельности
            if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                return ServiceResult<S3OperationResult>.Fail(ErrorMessages.ConcurrencyConflicts);

            throw;
        }
    }

    public async Task<ServiceResult<S3OperationResult>> DeleteObjectAsync(string key, Action<DeleteObjectRequest>? options = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        // Создаём запрос
        var deleteRequest = new DeleteObjectRequest()
        {
            BucketName = BucketName,
            Key = key
        };

        options?.Invoke(deleteRequest);

        try
        {
            // Вызов сервиса
            var response = await _client.DeleteObjectAsync(deleteRequest, ct);

            return ServiceResult<S3OperationResult>.Success(response.ToOperationResult());
        }
        catch (AmazonS3Exception ex)
        {
            // PreconditionFailed не случится, т.к DeleteObject не поддерживает Match-заголовки
            // NotFound не случится, т.к DeleteObject идемпотентный

            // Конфликт параллельности
            if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                return ServiceResult<S3OperationResult>.Fail(ErrorMessages.ConcurrencyConflicts);

            throw;
        }
    }

    public async Task<bool> IsObjectExistsAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            // Вызов сервиса
            var response = await _client.GetObjectMetadataAsync(BucketName, key, ct);

            return true;
        }
        catch (AmazonS3Exception ex)
        {
            // Объект не найден
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                return false;

            throw;
        }

        // Просто ебанная Санта-Барбара
        // Начнём с того, что если создать объект по ключу "avatars/default.png" через код или создать папку "avatars" и добавить "default.png" через UI - ЭТО НЕ ОДНО И ТОЖЕ
        // В первом случае папка "avatars/" НЕ СОЗДАЁТСЯ, в UI она просто отображается, но это не отдельный объект, а просто визуальный путь (если удалить файл, то и "папка" удалится вместе с ней)
        // Во-втором случае папка "avatars/" СОЗДАЁТСЯ, как реальный пустой объект, а не визуальный

        // GetObjectMetadataAsync (по смыслу объектного хранилища, это самый правильный способ)
        // - не сможет найти объект "avatars/" созданный через код (визуальная папка), внутри должны быть файлы (иначе папка вообще не может существовать)
        // - сможет найти объект "avatars/" созданный через UI (реальный пустой объект)
        // - генерирует исключение, если объект не найден

        // ListObjectsAsync (этот вариант ближе к файловой системы, но тонкости с префиксом ниже)
        // - не сможет найти объект "avatars/" созданный через код (визуальная папка), внутри должны быть файлы (иначе папка вообще не может существовать, +поиск по префиксу, т.е конкретно "avatars/" не найдётся, а файлы с этим префиксом найдутся, но нужны тонкости ниже)
        // - сможет найти объект "avatars/" созданный через UI (реальный пустой объект)
        // - не генерирует исключение
        // Тонкость: в методе используется префикс, а это значит, если ключ это "ava", то в ответе придёт "avatars/default.png"
        // Допустим у нас в S3 есть объект "test.png"
        // Если использовать префикс "te", то казалось бы, такого объекта несуществует, но в ответе всё равно будет "test.png" - т.е ответ True, хотя объекта с таким ключём нет
        // Поэтому нужно проверять вот так response.S3Objects.Any(x => x.Key == key), а не response.S3Objects.Count > 0, но тогда те же проблемы, как и с GetObjectMetadataAsync, конкретно объект "avatars/" не найдет, если он "визуальный"

        // Проверди эксперимент, создай дефолтную аватарку через код и удали только этот файл
        // О чудо, папка тоже удалится, хотя ты её не трогал

        // Очевидно, что GetObjectMetadataAsync обойдётся дешевле, чем ListObjectsAsync

        // S3 - это объектное хранилище, а не файловая система
        // Поэтому создание пустых объектов (папок), считается дурной практикой
        // И ещё, блять, прекрасно, что в S3 UI Beget нет возможности загрузить файл по ключу (чтобы не создавать папку) :)
        // Я написал простенькое консольное приложение для создания объектов по ключу - S3UI
        // Нужно обязательно прописывать путь из двух и более объектов, например "test/test.png". Загружаешь файл в папку "test", удаляешь "test.png", и готово. Папка "test" будет "визуальной"
    }

    public async Task<bool> CheckConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            await IsBucketExistsAsync(ct);
            return true;
        }
        catch (HttpRequestException ex)
        {
            if (ex.HttpRequestError == HttpRequestError.ConnectionError)
                return false;

            throw;
        }
    }

    /// <summary>
    /// Проверяет существует ли Bucket.
    /// </summary>
    private async Task<bool> IsBucketExistsAsync(CancellationToken ct = default)
    {
        // Создаём запрос
        var getBucketRequest = new GetBucketAclRequest()
        {
            BucketName = BucketName
        };

        try
        {
            // Вызов сервиса
            GetBucketAclResponse response = await _client.GetBucketAclAsync(getBucketRequest, ct);

            // Можно использовать AmazonS3Util.DoesS3BucketExistV2Async, но мне не понравилось, что почему-то в этом методе нельзя перебросить CT
            // Поэтому я просто оттуда скопировал реализацию и перебросил CT

            // Быстрее и дешевле, чем ListBucketsAsync

            return true;
        }
        catch (AmazonS3Exception ex)
        {
            switch (ex.ErrorCode)
            {
                case "AccessDenied":
                case "PermanentRedirect":
                    return true;
                case "NoSuchBucket":
                    return false; // Bucket'а не сущестует
                default:
                    throw;
            }
        }
    }

    [Obsolete("Метод не протестирован")]
    public async Task GetListBucketsAsync(CancellationToken ct = default)
    {
        ListBucketsResponse response = await _client.ListBucketsAsync(ct);
        foreach (S3Bucket bucket in response.Buckets)
            _logger.LogInformation("{bucket}\t{date}", bucket.BucketName, bucket.CreationDate);
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
            _logger.LogInformation("{key}\t{size}\t{lastModified}", o.Key, o.Size, o.LastModified);
    }
}