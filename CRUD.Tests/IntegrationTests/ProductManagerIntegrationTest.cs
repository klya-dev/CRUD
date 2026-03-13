using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace CRUD.Tests.IntegrationTests;

public class ProductManagerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly ApplicationDbContext _db;
    private readonly IProductManager _productManager;

    public ProductManagerIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _productManager = scopedServices.GetRequiredService<IProductManager>();
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
    }
    
    private IProductManager GenerateNewProductManager()
    {
        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        return scopedServices.GetRequiredService<IProductManager>();
    }

    [Fact] // Добавление продуктов, когда таблица пустая
    public async Task AddProductsToDbAsync_ShouldAdd_WhenTableEmpty()
    {
        // Arrange
        var productsFromDbBefore = await _db.Products.AsNoTracking().ToListAsync();

        // Act
        await _productManager.AddProductsToDbAsync();

        // Assert
        Assert.Empty(productsFromDbBefore);

        var productsFromDbAfter = await _db.Products.AsNoTracking().ToListAsync();
        Assert.NotEmpty(productsFromDbAfter);
    }

    [Fact] // Добавление продуктов, когда таблица не пустая
    public async Task AddProductsToDbAsync_ShouldNothing_WhenTableNotEmpty()
    {
        // Arrange
        // Добавляем продукты в базу
        await DI.CreateProductAsync(_db, name: Products.Premium);
        await DI.CreateProductAsync(_db, name: "something");

        var productsFromDbBefore = await _db.Products.AsNoTracking().ToListAsync();

        // Act
        await _productManager.AddProductsToDbAsync();

        // Assert
        var productsFromDbAfter = await _db.Products.AsNoTracking().ToListAsync();
        Assert.Equivalent(productsFromDbBefore, productsFromDbAfter);
    }


    // Конфликты параллельности


    [Fact] // Добавление продуктов, когда таблица пустая
    public async Task AddProductsToDbAsync_Concurrency_ShouldAdd_WhenTableEmpty()
    {
        // Arrange
        var productsFromDbBefore = await _db.Products.AsNoTracking().ToListAsync();
        var productManager = GenerateNewProductManager();
        var productManager2 = GenerateNewProductManager();

        // Act
        var task = productManager.AddProductsToDbAsync();
        var task2 = productManager2.AddProductsToDbAsync();

        // Может выбросится исключение с конфликтом параллельности, в документации это написано
        try
        {
            await Task.WhenAll(task, task2);

            // Assert
            Assert.Empty(productsFromDbBefore);

            var productsFromDbAfter = await _db.Products.AsNoTracking().ToListAsync();
            Assert.NotEmpty(productsFromDbAfter);
        }
        catch (DbUpdateException ex)
        {
            // Если не конфликт параллельности, не обрабатываем
            if (!DbExceptionHelper.IsConcurrencyConflict(ex))
                throw;
        }
    }

    [Fact] // Добавление продуктов, когда таблица не пустая
    public async Task AddProductsToDbAsync_Concurrency_ShouldNothing_WhenTableNotEmpty()
    {
        // Arrange
        // Добавляем продукты в базу
        await DI.CreateProductAsync(_db, name: Products.Premium);
        await DI.CreateProductAsync(_db, name: "something");

        var productsFromDbBefore = await _db.Products.AsNoTracking().ToListAsync();
        var productManager = GenerateNewProductManager();
        var productManager2 = GenerateNewProductManager();

        // Act
        var task = productManager.AddProductsToDbAsync();
        var task2 = productManager2.AddProductsToDbAsync();

        await Task.WhenAll(task, task2);

        // Assert
        var productsFromDbAfter = await _db.Products.AsNoTracking().ToListAsync();
        Assert.Equivalent(productsFromDbBefore, productsFromDbAfter);
    }
}