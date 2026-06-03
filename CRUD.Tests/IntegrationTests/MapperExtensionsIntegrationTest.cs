namespace CRUD.Tests.IntegrationTests;

public sealed class MapperExtensionsIntegrationTest
{
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
        var avatarPresignedUrl = "some";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;
        var userFromDb = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);

        var mustResult = new UserDto() { Firstname = userFromDb.Firstname, Username = userFromDb.Username, LanguageCode = userFromDb.LanguageCode, AvatarPresignedUrl = avatarPresignedUrl };

        // Act
        var result = userFromDb.ToUserDto(avatarPresignedUrl);

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(mustResult, result);
    }


    [Fact]
    public async Task ToUsersDto_ReturnsUsersDto()
    {
        // Arrange
        string[] avatarPresignedUrls = ["some", "some2"];

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем пользователя в базу
        var user2 = await DI.CreateUserAsync(_db, email: "test", phoneNumber: "123456789", username: "klya", ct: TestContext.Current.CancellationToken);

        var usersFromDb = await _db.Users.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);

        IEnumerable<UserDto> mustResult =
        [
            new UserDto { Firstname = user.Firstname, Username = user.Username, LanguageCode = user.LanguageCode, AvatarPresignedUrl = avatarPresignedUrls[0] },
            new UserDto { Firstname = user2.Firstname, Username = user2.Username, LanguageCode = user2.LanguageCode, AvatarPresignedUrl = avatarPresignedUrls[1] },
        ];

        // Act
        var result = usersFromDb.ToUsersDto(avatarPresignedUrls).OrderBy(x => x.Firstname);

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(mustResult, result);
    }

    [Fact]
    public async Task ToUsersDto_WhenAvatarPresignedUrlsNull_ReturnsUsersDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем пользователя в базу
        var user2 = await DI.CreateUserAsync(_db, email: "test", phoneNumber: "123456789", username: "klya", ct: TestContext.Current.CancellationToken);

        var usersFromDb = await _db.Users.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);

        var mustResult = usersFromDb.Select(x => new UserDto() { Firstname = x.Firstname, Username = x.Username, LanguageCode = x.LanguageCode, AvatarPresignedUrl = null });

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
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;
        var publicationFromDb = await _db.Publications.AsNoTracking().Include(x => x.User).FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);

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
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        var publicationIdGuid = publication.Id;
        var publicationFromDb = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationIdGuid, TestContext.Current.CancellationToken);

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
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;
        var publicationsFromDb = await _db.Publications.AsNoTracking().Include(x => x.User).Where(x => x.AuthorId == userIdGuid).ToListAsync(TestContext.Current.CancellationToken);
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
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        var publicationsFromDb = await _db.Publications.AsNoTracking().Where(x => x.AuthorId == userIdGuid).ToListAsync(TestContext.Current.CancellationToken);
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
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        var publicationsFromDb = await _db.Publications.AsNoTracking().Include(x => x.User).ToListAsync(TestContext.Current.CancellationToken);

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
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        // Добавляем публикацию в базу
        var publication2 = await DI.CreatePublicationAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        var publicationsFromDb = await _db.Publications.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);

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
}