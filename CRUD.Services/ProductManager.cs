using CRUD.Services.Interfaces;

namespace CRUD.Services;

/// <inheritdoc cref="IProductManager"/>
public class ProductManager : IProductManager
{
    private readonly ApplicationDbContext _db;
    private readonly IValidator<Product> _productValidator;

    public ProductManager(ApplicationDbContext db, IValidator<Product> productValidator)
    {
        _db = db;
        _productValidator = productValidator;
    }

    public async Task AddProductsToDbAsync(CancellationToken ct = default)
    {
        // Названия продуктов, которые уже в базе
        var productNamesFromDb = await _db.Products.AsNoTracking().Select(x => x.Name).ToListAsync(ct);

        // Продукты для добавления
        Product[] products =
        [ 
            new Product { Name = Products.Premium, Price = 1 }
        ];

        // Не добавляем в базу продукты, которые уже есть там
        foreach (var product in products)
            if (!productNamesFromDb.Contains(product.Name))
            {
                // Проверка валидности данных перед записью в базу
                var validationResult = await _productValidator.ValidateAsync(product, ct);
                if (!validationResult.IsValid)
                    throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(Product), validationResult.Errors));

                await _db.Products.AddAsync(product, ct);
            }

        await _db.SaveChangesAsync(ct);
    }
}