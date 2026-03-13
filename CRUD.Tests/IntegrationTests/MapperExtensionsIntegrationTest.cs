#nullable disable
using Microsoft.EntityFrameworkCore;

namespace CRUD.Tests.IntegrationTests;

public class MapperExtensionsIntegrationTest
{
    // #nullable disable

    private readonly ApplicationDbContext _db;

    public MapperExtensionsIntegrationTest()
    {
        ApplicationDbContext db = DbContextGenerator.GenerateDbContextTest();
        _db = db;
    }

    [Fact]
    public async Task ToUserDto_ReturnsUserDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var userIdGuid = user.Id;
        var userFromDb = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);

        var mustResult = new UserDto() { Firstname = userFromDb.Firstname, Username = userFromDb.Username, LanguageCode = userFromDb.LanguageCode };

        // Act
        var result = userFromDb.ToUserDto();

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(result, mustResult);
    }


    [Fact]
    public async Task ToUsersDto_ReturnsUsersDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем пользователя в базу
        var user2 = await DI.CreateUserAsync(_db, email: "test", phoneNumber: "123456789", username: "klya");

        var usersFromDb = await _db.Users.AsNoTracking().ToListAsync();

        var mustResult = usersFromDb.Select(x => new UserDto() { Firstname = x.Firstname, Username = x.Username, LanguageCode = x.LanguageCode });

        // Act
        var result = usersFromDb.ToUsersDto();

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(result, mustResult);
    }


    [Fact]
    public async Task ToPublicationDto_ReturnsPublicationDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        var publicationIdGuid = publication.Id;
        var publicationFromDb = await _db.Publications.AsNoTracking().Include(x => x.User).FirstOrDefaultAsync(x => x.Id == publicationIdGuid);

        var mustResult = new PublicationDto()
        {
            Id = publicationFromDb.Id,
            CreatedAt = publicationFromDb.CreatedAt.ToWithoutTicks(),
            EditedAt = publicationFromDb.EditedAt?.ToWithoutTicks(),
            Title = publicationFromDb.Title,
            Content = publicationFromDb.Content,
            AuthorId = publicationFromDb.AuthorId,
            AuthorFirstname = publicationFromDb.User.Firstname
        };

        // Act
        var result = publicationFromDb.ToPublicationDto(publicationFromDb.User.Firstname);

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(result, mustResult);
    }

    [Fact] // Если автор не прогружен
    public async Task ToPublicationDto_NotInclude_ReturnsPublicationDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        var publicationIdGuid = publication.Id;
        var publicationFromDb = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid);

        var mustResult = new PublicationDto()
        {
            Id = publicationFromDb.Id,
            CreatedAt = publicationFromDb.CreatedAt.ToWithoutTicks(),
            EditedAt = publicationFromDb.EditedAt?.ToWithoutTicks(),
            Title = publicationFromDb.Title,
            Content = publicationFromDb.Content,
            AuthorId = publicationFromDb.AuthorId,
            AuthorFirstname = "Автор удалён"
        };

        // Act
        var result = publicationFromDb.ToPublicationDto(publicationFromDb.User?.Firstname);

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(result, mustResult);
    }


    [Fact] // Две публикации одного автора
    public async Task ToPublicationsDto_ReturnsPublicationsDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id);

        var userIdGuid = user.Id;
        var publicationsFromDb = await _db.Publications.AsNoTracking().Include(x => x.User).Where(x => x.AuthorId == userIdGuid).ToListAsync();
        var authorName = publicationsFromDb.FirstOrDefault().User.Firstname;

        var mustResult = publicationsFromDb.Select(x => new PublicationDto()
        {
            Id = x.Id,
            CreatedAt = x.CreatedAt.ToWithoutTicks(),
            EditedAt = x.EditedAt?.ToWithoutTicks(),
            Title = x.Title,
            Content = x.Content,
            AuthorId = x.AuthorId,
            AuthorFirstname = x.User?.Firstname
        });

        // Act
        var result = publicationsFromDb.ToPublicationsDto(authorName);

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(result, mustResult);
    }

    [Fact]  // Две публикации одного автора
    public async Task ToPublicationsDto_NotInclude_ReturnsPublicationsDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id);

        var userIdGuid = user.Id;

        var publicationsFromDb = await _db.Publications.AsNoTracking().Where(x => x.AuthorId == userIdGuid).ToListAsync();
        var authorName = publicationsFromDb.FirstOrDefault().User?.Firstname;

        var mustResult = publicationsFromDb.Select(x => new PublicationDto()
        {
            Id = x.Id,
            CreatedAt = x.CreatedAt.ToWithoutTicks(),
            EditedAt = x.EditedAt?.ToWithoutTicks(),
            Title = x.Title,
            Content = x.Content,
            AuthorId = x.AuthorId,
            AuthorFirstname = "Автор удалён"
        });

        // Act
        var result = publicationsFromDb.ToPublicationsDto(authorName);

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(result, mustResult);
    }


    [Fact]
    public async Task ToPublicationsDtoByFunc_ReturnsPublicationsDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id);

        var publicationsFromDb = await _db.Publications.AsNoTracking().Include(x => x.User).ToListAsync();

        var mustResult = publicationsFromDb.Select(x => new PublicationDto()
        {
            Id = x.Id,
            CreatedAt = x.CreatedAt.ToWithoutTicks(),
            EditedAt = x.EditedAt?.ToWithoutTicks(),
            Title = x.Title,
            Content = x.Content,
            AuthorId = x.AuthorId,
            AuthorFirstname = x.User?.Firstname ?? "Автор удалён"
        });

        // Act
        var result = publicationsFromDb.ToPublicationsDto(x => x.User?.Firstname);

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(result, mustResult);
    }

    [Fact]
    public async Task ToPublicationsDtoByFunc_NotInclude_ReturnsPublicationsDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id);

        var publicationsFromDb = await _db.Publications.AsNoTracking().ToListAsync();

        var mustResult = publicationsFromDb.Select(x => new PublicationDto()
        {
            Id = x.Id,
            CreatedAt = x.CreatedAt.ToWithoutTicks(),
            EditedAt = x.EditedAt?.ToWithoutTicks(),
            Title = x.Title,
            Content = x.Content,
            AuthorId = x.AuthorId,
            AuthorFirstname = "Автор удалён"
        });

        // Act
        var result = publicationsFromDb.ToPublicationsDto(x => x.User?.Firstname);

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(result, mustResult);
    }


    // Конфликты параллельности


    [Fact]
    public async Task ToUserDto_ConcurrencyConflict_ReturnsUserDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var userIdGuid = user.Id;
        var userFromDb = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);

        var mustResult = new UserDto() { Firstname = userFromDb.Firstname, Username = userFromDb.Username, LanguageCode = userFromDb.LanguageCode };

        // Act
        var task = Task.Run(() => userFromDb.ToUserDto());
        var task2 = Task.Run(() => userFromDb.ToUserDto());

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(result, mustResult);

        Assert.Equivalent(result, result2);
    }


    [Fact]
    public async Task ToUsersDto_ConcurrencyConflict_ReturnsUsersDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем пользователя в базу
        var user2 = await DI.CreateUserAsync(_db, email: "test", phoneNumber: "123456789", username: "klya");

        var usersFromDb = await _db.Users.AsNoTracking().ToListAsync();

        var mustResult = usersFromDb.Select(x => new UserDto() { Firstname = x.Firstname, Username = x.Username, LanguageCode = x.LanguageCode });

        // Act
        var task = Task.Run(() => usersFromDb.ToUsersDto());
        var task2 = Task.Run(() => usersFromDb.ToUsersDto());

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(result, mustResult);

        Assert.Equivalent(result, result2);
    }


    [Fact]
    public async Task ToPublicationDto_ConcurrencyConflict_ReturnsPublicationDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        var publicationIdGuid = publication.Id;
        var publicationFromDb = await _db.Publications.AsNoTracking().Include(x => x.User).FirstOrDefaultAsync(x => x.Id == publicationIdGuid);

        var mustResult = new PublicationDto()
        {
            Id = publicationFromDb.Id,
            CreatedAt = publicationFromDb.CreatedAt.ToWithoutTicks(),
            EditedAt = publicationFromDb.EditedAt?.ToWithoutTicks(),
            Title = publicationFromDb.Title,
            Content = publicationFromDb.Content,
            AuthorId = publicationFromDb.AuthorId,
            AuthorFirstname = publicationFromDb.User.Firstname
        };

        // Act
        var task = Task.Run(() => publicationFromDb.ToPublicationDto(publicationFromDb.User.Firstname));
        var task2 = Task.Run(() => publicationFromDb.ToPublicationDto(publicationFromDb.User.Firstname));

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(result, mustResult);

        Assert.Equivalent(result, result2);
    }

    [Fact] // Если автор не прогружен
    public async Task ToPublicationDto_ConcurrencyConflict_NotInclude_ReturnsPublicationDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        var publicationIdGuid = publication.Id;
        var publicationFromDb = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid);

        var mustResult = new PublicationDto()
        {
            Id = publicationFromDb.Id,
            CreatedAt = publicationFromDb.CreatedAt.ToWithoutTicks(),
            EditedAt = publicationFromDb.EditedAt?.ToWithoutTicks(),
            Title = publicationFromDb.Title,
            Content = publicationFromDb.Content,
            AuthorId = publicationFromDb.AuthorId,
            AuthorFirstname = "Автор удалён"
        };

        // Act
        var task = Task.Run(() => publicationFromDb.ToPublicationDto(publicationFromDb.User?.Firstname));
        var task2 = Task.Run(() => publicationFromDb.ToPublicationDto(publicationFromDb.User?.Firstname));

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(result, mustResult);

        Assert.Equivalent(result, result2);
    }


    [Fact] // Две публикации одного автора
    public async Task ToPublicationsDto_ConcurrencyConflict_ReturnsPublicationsDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id);

        var userIdGuid = user.Id;
        var publicationsFromDb = await _db.Publications.AsNoTracking().Include(x => x.User).Where(x => x.AuthorId == userIdGuid).ToListAsync();
        var authorName = publicationsFromDb.FirstOrDefault().User.Firstname;

        var mustResult = publicationsFromDb.Select(x => new PublicationDto()
        {
            Id = x.Id,
            CreatedAt = x.CreatedAt.ToWithoutTicks(),
            EditedAt = x.EditedAt?.ToWithoutTicks(),
            Title = x.Title,
            Content = x.Content,
            AuthorId = x.AuthorId,
            AuthorFirstname = x.User?.Firstname
        });

        // Act
        var task = Task.Run(() => publicationsFromDb.ToPublicationsDto(authorName));
        var task2 = Task.Run(() => publicationsFromDb.ToPublicationsDto(authorName));

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(result, mustResult);

        Assert.Equivalent(result, result2);
    }

    [Fact] // Две публикации одного автора
    public async Task ToPublicationsDto_ConcurrencyConflict_NotInclude_ReturnsPublicationsDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id);

        var userIdGuid = user.Id;
        var publicationsFromDb = await _db.Publications.AsNoTracking().Where(x => x.AuthorId == userIdGuid).ToListAsync();
        var authorName = publicationsFromDb.FirstOrDefault().User?.Firstname;

        var mustResult = publicationsFromDb.Select(x => new PublicationDto()
        {
            Id = x.Id,
            CreatedAt = x.CreatedAt.ToWithoutTicks(),
            EditedAt = x.EditedAt?.ToWithoutTicks(),
            Title = x.Title,
            Content = x.Content,
            AuthorId = x.AuthorId,
            AuthorFirstname = "Автор удалён"
        });

        // Act
        var task = Task.Run(() => publicationsFromDb.ToPublicationsDto(authorName));
        var task2 = Task.Run(() => publicationsFromDb.ToPublicationsDto(authorName));

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(result, mustResult);

        Assert.Equivalent(result, result2);
    }


    [Fact]
    public async Task ToPublicationsDtoByFunc_ConcurrencyConflict_ReturnsPublicationsDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id);
        var publicationsFromDb = await _db.Publications.AsNoTracking().Include(x => x.User).ToListAsync();

        var mustResult = publicationsFromDb.Select(x => new PublicationDto()
        {
            Id = x.Id,
            CreatedAt = x.CreatedAt.ToWithoutTicks(),
            EditedAt = x.EditedAt?.ToWithoutTicks(),
            Title = x.Title,
            Content = x.Content,
            AuthorId = x.AuthorId,
            AuthorFirstname = x.User?.Firstname ?? "Автор удалён"
        });

        // Act
        var task = Task.Run(() => publicationsFromDb.ToPublicationsDto(x => x.User?.Firstname));
        var task2 = Task.Run(() => publicationsFromDb.ToPublicationsDto(x => x.User?.Firstname));

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(result, mustResult);

        Assert.Equivalent(result, result2);
    }

    [Fact]
    public async Task ToPublicationsDtoByFunc_ConcurrencyConflict_NotInclude_ReturnsPublicationsDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id);

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id);

        var publicationsFromDb = await _db.Publications.AsNoTracking().ToListAsync();

        var mustResult = publicationsFromDb.Select(x => new PublicationDto()
        {
            Id = x.Id,
            CreatedAt = x.CreatedAt.ToWithoutTicks(),
            EditedAt = x.EditedAt?.ToWithoutTicks(),
            Title = x.Title,
            Content = x.Content,
            AuthorId = x.AuthorId,
            AuthorFirstname = "Автор удалён"
        });

        // Act
        var task = Task.Run(() => publicationsFromDb.ToPublicationsDto(x => x.User?.Firstname));
        var task2 = Task.Run(() => publicationsFromDb.ToPublicationsDto(x => x.User?.Firstname));

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(result, mustResult);

        Assert.Equivalent(result, result2);
    }
}