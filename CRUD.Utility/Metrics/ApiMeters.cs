using System.Diagnostics.Metrics;

namespace CRUD.Utility.Metrics;

/// <summary>
/// Пользовательские метрики приложения.
/// </summary>
public class ApiMeters
{
    public const string MeterName = "CRUD.WebApi.Meters";
    public const string ProductIssueMeterName = "crud.product.issue";
    public const string UsefulNotificationMeterName = "crud.usefulnotification";

    private readonly Counter<int> _productIssueCounter;
    private readonly Counter<int> _usefulNotificationCounter;

    public ApiMeters(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _productIssueCounter = meter.CreateCounter<int>(ProductIssueMeterName);
        _usefulNotificationCounter = meter.CreateCounter<int>(UsefulNotificationMeterName);
    }

    /// <summary>
    /// Добавляет в счётчик телеметрии выданный продукт.
    /// </summary>
    /// <param name="productName">Название продукта. <see cref="Products"/>.</param>
    public void IssueProduct(string productName)
    {
        _productIssueCounter.Add(1, new KeyValuePair<string, object?>("product", productName));
    }

    /// <summary>
    /// Добавляет в счётчик телеметрии полезность уведомления.
    /// </summary>
    /// <param name="notificationId">Id уведомления.</param>
    public void UsefulNotification(Guid notificationId)
    {
        _usefulNotificationCounter.Add(1, new KeyValuePair<string, object?>("notificationId", notificationId));
    }
}