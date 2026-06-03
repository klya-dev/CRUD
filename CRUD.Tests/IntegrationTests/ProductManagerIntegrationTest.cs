using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace CRUD.Tests.IntegrationTests;

public sealed class ProductManagerIntegrationTest : IClassFixture<TestWebApplicationFactory>
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
    
    [Fact] // Добавление продуктов, когда таблица пустая
    public async Task AddProductsToDbAsync_ShouldAdd_WhenTableEmpty()
    {
        // Arrange
        var productsFromDbBefore = await _db.Products.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);

        // Act
        await _productManager.AddProductsToDbAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(productsFromDbBefore);

        var productsFromDbAfter = await _db.Products.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        Assert.NotEmpty(productsFromDbAfter);
    }

    [Fact] // Добавление продуктов, когда таблица не пустая
    public async Task AddProductsToDbAsync_ShouldNothing_WhenTableNotEmpty()
    {
        // Arrange
        // Добавляем продукты в базу
        await DI.CreateProductAsync(_db, name: Products.Premium, ct: TestContext.Current.CancellationToken);
        await DI.CreateProductAsync(_db, name: "something", ct: TestContext.Current.CancellationToken);

        var productsFromDbBefore = await _db.Products.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);

        // Act
        await _productManager.AddProductsToDbAsync(TestContext.Current.CancellationToken);

        // Assert
        var productsFromDbAfter = await _db.Products.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equivalent(productsFromDbBefore, productsFromDbAfter);
    }
}