using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CRUD.Services;

/// <inheritdoc cref="IPayManager"/>
public class PayManager : IPayManager
{
    private readonly string URL;
    private readonly string ShopId;
    private readonly string ApiKey;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PayManager> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _db;
    private readonly IOrderCreator _orderCreator;

    public PayManager(IOptions<PayManagerOptions> options, IHttpClientFactory httpClientFactory, ILogger<PayManager> logger, IHttpContextAccessor httpContextAccessor, ApplicationDbContext db, IOrderCreator orderCreator)
    {
        URL = options.Value.ServiceURL;
        ShopId = options.Value.ShopId;
        ApiKey = options.Value.ApiKey;

        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _db = db;
        _orderCreator = orderCreator;
    }

    public async Task<PaymentResponse?> PayAsync(string productName, Guid userId, CancellationToken ct = default)
    {
        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Продукт не найден
        var productFromDb = await _db.Products.AsNoTracking().Where(x => x.Name == productName).Select(x => new { x.Price }).FirstOrDefaultAsync(ct);
        if (productFromDb == null)
        {
            _logger.LogError("Не удалось найти продукт. {productName}.", productName);
            return null;
        }

        // Пользователь не найден
        var userExists = await _db.Users.AnyAsync(x => x.Id == userId, ct);
        if (!userExists)
        {
            _logger.LogError("Не удалось найти пользователя. {userId}.", userId);
            return null;
        }

        // Создание клиента
        var httpClient = _httpClientFactory.CreateClient(HttpClientNames.PayManager); // using необязателен, IHttpClientFactory сам вызывает Dispose (https://learn.microsoft.com/ru-ru/aspnet/core/fundamentals/http-requests?view=aspnetcore-9.0#httpclient-and-lifetime-management | https://stackoverflow.com/questions/50912160/should-httpclient-instances-created-by-httpclientfactory-be-disposed)

        // Создаём запрос
        var url = "payments";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        // Авторизация уже настроена в фабрике IHttpClientFactory

        // Тело запроса
        var baseUrl = _httpContextAccessor.GetBaseUrl(); // Просто домашняя страница
        var orderNumber = await _orderCreator.GetOrderNumberAsync(ct);
        var body = new
        {
            amount = new { value = productFromDb.Price.ToString("0.00", CultureInfo.InvariantCulture), currency = "RUB" },
            confirmation = new { type = "redirect", return_url = baseUrl },
            description = $"Заказ №{orderNumber}",
            capture = true, // Мне не нужно подтверждать платёж, сразу списание
        };
        var json = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, Application.Json);
        request.Content = json;

        // Отправляем запрос (платёж)
        using var response = await httpClient.SendAsync(request, ct);

        // Читаем содержимое ответа
        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: ct);

        // Неуспешный ответ
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Не удалось создать платёж. \"{description}\".", jsonDocument.RootElement.GetProperty("description"));
            return null;
        }

        // Достаём ссылку для оплаты
        var confirmationUrl = jsonDocument.RootElement.GetProperty("confirmation").GetProperty("confirmation_url").GetString();
        if (confirmationUrl == null)
        {
            _logger.LogError("Не удалось создать платёж. Т.к. в ответе нет \"confirmation_url\".");
            return null;
        }

        // Парсим ответ в модель
        PaymentResponse? paymentResponse = _orderCreator.GetPaymentResponseFromApi(jsonDocument);
        if (paymentResponse == null)
        {
            _logger.LogError("Не удалось пропарсить ответ в модель. Json: \"{json}\".", jsonDocument.RootElement);
            return null;
        }

        // Добавляем заказ в базу
        await _orderCreator.AddOrderToDbAsync(paymentResponse, userId, productName, ct);

        return paymentResponse;
    }

    public async Task<bool> CheckConnectionAsync(CancellationToken ct = default)
    {
        // Создание клиента
        var httpClient = _httpClientFactory.CreateClient(HttpClientNames.PayManager);

        // Создаём запрос
        var url = "refunds"; // У ЮКассы нет отдельного метода для проверки подключения. Использую "refunds", т.к у меня нет функционала на возврат, значит, там данных не должно быть - быстрый ответ
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        // Авторизация уже настроена в фабрике IHttpClientFactory

        // Отправляем запрос
        using var response = await httpClient.SendAsync(request, ct);

        // Неуспешный ответ
        if (!response.IsSuccessStatusCode)
        {
            // Читаем содержимое ответа
            await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
            using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: ct);

            _logger.LogError("Не удалось подключится к платёжному серверу. По причине: \"{description}\".", jsonDocument.RootElement.GetString("description"));
            return false;
        }

        return true;
    }
}