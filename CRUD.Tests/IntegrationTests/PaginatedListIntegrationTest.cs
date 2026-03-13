using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace CRUD.Tests.IntegrationTests;

public class PaginatedListIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    // #nullable disable

    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly ApplicationDbContext _db;

    public PaginatedListIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
    }

    [Fact] // Корректные данные
    public async Task CreateAsync_WhenPageIndex1AndSize2_ReturnsPaginatedList()
    {
        // Arrange
        int pageIndex = 1;
        int pageSize = 2;

        // Добавляем автора в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication3 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication4 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication5 = await DI.CreatePublicationAsync(_db, null);

        var publicationsFromDb = _db.Publications.AsNoTracking().OrderBy(x => x.CreatedAt).Select(x => x.ToPublicationDto(x.User!.Firstname));

        // Такой результат должен быть
        var mustResult = new List<PublicationDto>()
        {
            new PublicationDto
            {
                Id = publication.Id,
                CreatedAt = publication.CreatedAt.ToWithoutTicks(),
                EditedAt = publication.EditedAt?.ToWithoutTicks(),
                Title = publication.Title,
                Content = publication.Content,
                AuthorId = publication.AuthorId,
                AuthorFirstname = user.Firstname
            },
            new PublicationDto
            {
                Id = publication2.Id,
                CreatedAt = publication2.CreatedAt.ToWithoutTicks(),
                EditedAt = publication2.EditedAt?.ToWithoutTicks(),
                Title = publication2.Title,
                Content = publication2.Content,
                AuthorId = publication2.AuthorId,
                AuthorFirstname = user.Firstname
            }
        };

        // Act
        var result = await PaginatedList<PublicationDto>.CreateAsync(publicationsFromDb, pageIndex, pageSize);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        Assert.Equivalent(mustResult, result.ToList());
        Assert.Equal(pageIndex, result.PageIndex);
        Assert.Equal(pageSize, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        Assert.Null(result.SearchString);
        Assert.Null(result.SortBy);
        Assert.False(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact] // Корректные данные
    public async Task CreateAsync_WhenPageIndex2AndSize3_ReturnsPaginatedList()
    {
        // Arrange
        int pageIndex = 2;
        int pageSize = 3;

        // Добавляем автора в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication3 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication4 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication5 = await DI.CreatePublicationAsync(_db, null);

        var publicationsFromDb = _db.Publications.AsNoTracking().OrderBy(x => x.CreatedAt).Select(x => x.ToPublicationDto(x.User!.Firstname));

        // Такой результат должен быть
        var mustResult = new List<PublicationDto>()
        {
            new PublicationDto
            {
                Id = publication4.Id,
                CreatedAt = publication4.CreatedAt.ToWithoutTicks(),
                EditedAt = publication4.EditedAt?.ToWithoutTicks(),
                Title = publication4.Title,
                Content = publication4.Content,
                AuthorId = publication4.AuthorId,
                AuthorFirstname = user.Firstname
            },
            new PublicationDto
            {
                Id = publication5.Id,
                CreatedAt = publication5.CreatedAt.ToWithoutTicks(),
                EditedAt = publication5.EditedAt?.ToWithoutTicks(),
                Title = publication5.Title,
                Content = publication5.Content,
                AuthorId = publication5.AuthorId,
                AuthorFirstname = "Автор удалён"
            }
        };

        // Act
        var result = await PaginatedList<PublicationDto>.CreateAsync(publicationsFromDb, pageIndex, pageSize);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        Assert.Equivalent(mustResult, result.ToList());
        Assert.Equal(pageIndex, result.PageIndex);
        Assert.Equal(pageSize, result.PageSize);
        Assert.Equal(2, result.TotalPages);
        Assert.Null(result.SearchString);
        Assert.Null(result.SortBy);
        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact] // Корректные данные
    public async Task CreateAsync_WhenPageIndex1AndSize5_ReturnsPaginatedList()
    {
        // Arrange
        int pageIndex = 1;
        int pageSize = 5;

        // Добавляем автора в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication3 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication4 = await DI.CreatePublicationAsync(_db, user.Id);
        var publication5 = await DI.CreatePublicationAsync(_db, null);

        var publicationsFromDb = _db.Publications.AsNoTracking().OrderBy(x => x.CreatedAt).Select(x => x.ToPublicationDto(x.User!.Firstname));

        // Такой результат должен быть
        var mustResult = new List<PublicationDto>()
        {
            new PublicationDto
            {
                Id = publication.Id,
                CreatedAt = publication.CreatedAt.ToWithoutTicks(),
                EditedAt = publication.EditedAt?.ToWithoutTicks(),
                Title = publication.Title,
                Content = publication.Content,
                AuthorId = publication.AuthorId,
                AuthorFirstname = user.Firstname
            },
            new PublicationDto
            {
                Id = publication2.Id,
                CreatedAt = publication2.CreatedAt.ToWithoutTicks(),
                EditedAt = publication2.EditedAt?.ToWithoutTicks(),
                Title = publication2.Title,
                Content = publication2.Content,
                AuthorId = publication2.AuthorId,
                AuthorFirstname = user.Firstname
            },
            new PublicationDto
            {
                Id = publication3.Id,
                CreatedAt = publication3.CreatedAt.ToWithoutTicks(),
                EditedAt = publication3.EditedAt?.ToWithoutTicks(),
                Title = publication3.Title,
                Content = publication3.Content,
                AuthorId = publication3.AuthorId,
                AuthorFirstname = user.Firstname
            },
            new PublicationDto
            {
                Id = publication4.Id,
                CreatedAt = publication4.CreatedAt.ToWithoutTicks(),
                EditedAt = publication4.EditedAt?.ToWithoutTicks(),
                Title = publication4.Title,
                Content = publication4.Content,
                AuthorId = publication4.AuthorId,
                AuthorFirstname = user.Firstname
            },
            new PublicationDto
            {
                Id = publication5.Id,
                CreatedAt = publication5.CreatedAt.ToWithoutTicks(),
                EditedAt = publication5.EditedAt?.ToWithoutTicks(),
                Title = publication5.Title,
                Content = publication5.Content,
                AuthorId = publication5.AuthorId,
                AuthorFirstname = "Автор удалён"
            }
        };

        // Act
        var result = await PaginatedList<PublicationDto>.CreateAsync(publicationsFromDb, pageIndex, pageSize);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        Assert.Equivalent(mustResult, result.ToList());
        Assert.Equal(pageIndex, result.PageIndex);
        Assert.Equal(pageSize, result.PageSize);
        Assert.Equal(1, result.TotalPages);
        Assert.Null(result.SearchString);
        Assert.Null(result.SortBy);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }
}