using CRUD.Services.Interfaces;

namespace CRUD.Services;

/// <inheritdoc cref="IProductManager"/>
public sealed class ProductManager : IProductManager
{
    private readonly ApplicationDbContext _db;

    public ProductManager(ApplicationDbContext db)
    {
        _db = db;
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
                await _db.Products.AddAsync(product, ct);

        await _db.SaveChangesAsync(ct);
    }
}