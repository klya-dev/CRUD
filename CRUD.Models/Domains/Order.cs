namespace CRUD.Models.Domains;

/// <summary>
/// Domain модель заказа.
/// </summary>
public class Order
{
    /// <summary>
    /// Id заказа.
    /// </summary>
    /// <remarks>
    /// Генерируется на стороне провайдера.
    /// </remarks>
    public required Guid Id { get; set; }

    /// <summary>
    /// Id пользователя.
    /// </summary>
    public required Guid? UserId { get; set; }

    /// <summary>
    /// Сущность пользователя.
    /// </summary>
    /// <remarks>
    /// Необходимо прогружать по <see cref="UserId"/>.
    /// </remarks>
    public User? User { get; set; }

    /// <summary>
    /// Статус заказа.
    /// </summary>
    /// <remarks>
    /// Из констант <see cref="OrderStatuses"/>.
    /// </remarks>
    public required string Status { get; set; }

    /// <summary>
    /// Статус оплаты.
    /// </summary>
    /// <remarks>
    /// Из констант <see cref="PaymentStatuses"/>.
    /// </remarks>
    public required string PaymentStatus { get; set; }

    /// <summary>
    /// Продукт заказа.
    /// </summary>
    /// <remarks>
    /// Из констант <see cref="Products"/>.
    /// </remarks>
    public required string ProductName { get; set; }

    /// <summary>
    /// Сущность продукта.
    /// </summary>
    /// <remarks>
    /// Необходимо прогружать по <see cref="ProductName"/>.
    /// </remarks>
    [NotMapped]
    public Product? Product { get; set; }

    /// <summary>
    /// Оплачен ли заказ.
    /// </summary>
    public required bool Paid { get; set; } = false;

    /// <summary>
    /// Сумма заказа.
    /// </summary>
    public required decimal Amount { get; set; } // У ЮКассы сумма это string, но я решил у себя хранить, как дробный тип (и decimal вместо double это мастхев)

    /// <summary>
    /// Валюта заказа.
    /// </summary>
    public required string Currency { get; set; }

    /// <summary>
    /// Дата создания заказа.
    /// </summary>
    public required DateTime CreatedAt { get; set; }

    /// <summary>
    /// Описание заказа.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Можно ли вернуть деньги за заказ.
    /// </summary>
    public required bool Refundable { get; set; }

    /// <summary>
    /// Версия данных заказа, при каждом обновлении данных заказа, обновляется.
    /// </summary>
    /// <remarks>
    /// Используется для решения конфликтов параллельности.
    /// </remarks>
    public byte[]? RowVersion { get; set; }
}