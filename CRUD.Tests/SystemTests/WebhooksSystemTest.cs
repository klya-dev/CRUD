using CRUD.Utility.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.Metrics;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CRUD.Tests.SystemTests;

public class WebhooksSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;
    private readonly IMeterFactory _meterFactory;

    public WebhooksSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
        _meterFactory = scopedServices.GetRequiredService<IMeterFactory>();
    }

    [Fact]
    public async Task Post_Payment_ReturnsOk()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureAppConfiguration((ctx, config) =>
            {
                var dict = new Dictionary<string, string>
                {
                    [$"{PayManagerOptions.SectionName}:{nameof(PayManagerOptions.SafeListIp)}"] = "127.0.0.1" // В SafeListIp будет только этот IP-адрес
                };
                config.AddInMemoryCollection(dict);
            });
        }).CreateClient(); // +ниже добавляем заголовок

        // Почему-то есть использовать WithWebHostBuilder, то метрики не работают
        //var collector = new MetricCollector<int>(_meterFactory, ApiMeters.MeterName, ApiMeters.ProductIssueMeterName);
        //var collector = new MetricCollector<double>(_meterFactory, "Microsoft.AspNetCore.Hosting", "http.server.request.duration");

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем продукт в базу
        await DI.CreateProductAsync(_db);

        // Добавляем заказ в базу
        var order = await DI.CreateOrderAsync(_db, user.Id, status: OrderStatuses.Accept, paymentStatus: PaymentStatuses.Pending);

        var orderIdGuid = order.Id;

        var data = new PaymentWebHook()
        {
            Type = "notification",
            Event = "payment." + PaymentStatuses.Succeeded,
            Object = new { id = orderIdGuid, status = PaymentStatuses.Succeeded, paid = true }
        };

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.WEBHOOKS_PAYMENT_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        request.Headers.Add("X-Forwarded-For", ["127.0.0.1", "127.0.0.1"]); // Якобы 127.0.0.1 вместо первого отправителя

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Данные заказа обновились и товар выдан
        var orderFromDbAfter = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == orderIdGuid);
        Assert.Equal(PaymentStatuses.Succeeded, orderFromDbAfter.PaymentStatus);
        Assert.Equal(OrderStatuses.Done, orderFromDbAfter.Status);
        Assert.True(orderFromDbAfter.Paid);

        // Метрика добавилась
        //await collector.WaitForMeasurementsAsync(minCount: 1).WaitAsync(TimeSpan.FromSeconds(5));
        //Assert.Collection(collector.GetMeasurementSnapshot(),
        //    measurement =>
        //    {
        //        Assert.Equal(Products.Premium, measurement.Tags["product"]);
        //        Assert.Equal(1, measurement.Value);
        //    });
    }

    [Fact]
    public async Task Post_Payment_ReturnsOrderNotFound()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureAppConfiguration((ctx, config) =>
            {
                var dict = new Dictionary<string, string>
                {
                    [$"{PayManagerOptions.SectionName}:{nameof(PayManagerOptions.SafeListIp)}"] = "127.0.0.1" // В SafeListIp будет только этот IP-адрес
                };
                config.AddInMemoryCollection(dict);
            });
        }).CreateClient();  // +ниже добавляем заголовок

        // Добавляем продукт в базу
        await DI.CreateProductAsync(_db);
        var orderIdGuid = Guid.NewGuid();

        var data = new PaymentWebHook()
        {
            Type = "notification",
            Event = "payment." + PaymentStatuses.Succeeded,
            Object = new { id = orderIdGuid, status = PaymentStatuses.Succeeded, paid = true }
        };

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.WEBHOOKS_PAYMENT_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        request.Headers.Add("X-Forwarded-For", ["127.0.0.1", "127.0.0.1"]); // Якобы 127.0.0.1 вместо первого отправителя

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.ORDER_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    // Конфликты параллельности


    [Fact]
    public async Task Post_Payment_ConcurrencyConflict_ReturnsOkOrConflictOrOrderAlreadyIssuedOrCanceledOrUserAlreadyHasPremium()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureAppConfiguration((ctx, config) =>
            {
                var dict = new Dictionary<string, string>
                {
                    [$"{PayManagerOptions.SectionName}:{nameof(PayManagerOptions.SafeListIp)}"] = "127.0.0.1" // В SafeListIp будет только этот IP-адрес
                };
                config.AddInMemoryCollection(dict);
            });
        }).CreateClient(); // +ниже добавляем заголовок
        var client2 = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureAppConfiguration((ctx, config) =>
            {
                var dict = new Dictionary<string, string>
                {
                    [$"{PayManagerOptions.SectionName}:{nameof(PayManagerOptions.SafeListIp)}"] = "127.0.0.1" // В SafeListIp будет только этот IP-адрес
                };
                config.AddInMemoryCollection(dict);
            });
        }).CreateClient();

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем продукт в базу
        await DI.CreateProductAsync(_db);

        // Добавляем заказ в базу
        var order = await DI.CreateOrderAsync(_db, user.Id, status: OrderStatuses.Accept, paymentStatus: PaymentStatuses.Pending);

        var orderIdGuid = order.Id;

        // Данные для запросов
        var data = new PaymentWebHook()
        {
            Type = "notification",
            Event = "payment." + PaymentStatuses.Succeeded,
            Object = new { id = orderIdGuid, status = PaymentStatuses.Succeeded, paid = true }
        };

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.WEBHOOKS_PAYMENT_URL);
        var json = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request.Content = json;
        request.Headers.Add("X-Forwarded-For", ["127.0.0.1", "127.0.0.1"]); // Якобы 127.0.0.1 вместо первого отправителя

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Post, TestConstants.WEBHOOKS_PAYMENT_URL);
        var json2 = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, Application.Json);
        request2.Content = json2;
        request2.Headers.Add("X-Forwarded-For", ["127.0.0.1", "127.0.0.1"]);

        // Act
        using var task = client.SendAsync(request);
        using var task2 = client2.SendAsync(request2);

        var results = await Task.WhenAll(task, task2);

        // Assert
        foreach (var result in results)
        {
            Assert.NotNull(result);

            // Ошибка сервера
            if (System.Net.HttpStatusCode.InternalServerError == result.StatusCode)
                Assert.Fail("InternalServerError");

            // Может быть успешный ответ
            if (System.Net.HttpStatusCode.OK == result.StatusCode)
            {
                Assert.Null(result.Content.Headers.ContentType);

                // Данные заказа обновились и товар выдан
                var orderFromDbAfter = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == orderIdGuid);
                Assert.Equal(PaymentStatuses.Succeeded, orderFromDbAfter.PaymentStatus);
                Assert.Equal(OrderStatuses.Done, orderFromDbAfter.Status);
                Assert.True(orderFromDbAfter.Paid);

                continue;
            }

            // Читаем содержимое ответа
            await using var contentStream = await result.Content.ReadAsStreamAsync();
            using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

            // Может быть неуспешный ответ
            if (!result.IsSuccessStatusCode)
            {
                // Либо заказ выдан или отменён, либо у пользователя уже есть премиум, либо Conflict
                var errorCode = jsonDocument.RootElement.GetProperty("code").GetString();
                string[] allowedErrors =
                [
                    ErrorCodes.ORDER_ALREADY_ISSUED_OR_CANCELED,
                    ErrorCodes.USER_ALREADY_HAS_PREMIUM,
                    ErrorCodes.CONCURRENCY_CONFLICTS
                ];

                Assert.Contains(errorCode, allowedErrors);
            }
        }
    }
}