namespace CRUD.Tests.IntegrationTests;

[Collection(nameof(IntegrationsTestCollection))]
public sealed class CursorBasedPaginatedPublicationsIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IPublicationManager _publicationManager;
    private readonly ApplicationDbContext _db;

    public CursorBasedPaginatedPublicationsIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _publicationManager = scopedServices.GetRequiredService<IPublicationManager>();
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
    }

    // Пять статей, три последовательных запроса, пайплайн работает корректно (пагинация курсором)
    // Sort: date_desc (от новой к старой)
    [Fact]
    public async Task CursorBasedPipeline_Sort_DateDesc()
    {
        // Arrange
        DateTime? date = null;
        Guid? lastId = null;
        int limit = 2;
        string searchString = null;
        string sortBy = SortByVariables.date_desc;

        // Добавляем пользователей в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var user2 = await DI.CreateUserAsync(_db, username: "test", email: "test", phoneNumber: "123", ct: TestContext.Current.CancellationToken);

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, DateTime.UtcNow, "Первые шаги", "Работа с REST API и отправка первых запросов.", null, TestContext.Current.CancellationToken);
        var publication2 = await DI.CreatePublicationAsync(_db, user2.Id, DateTime.UtcNow.AddDays(-1), "Ошибки", "Коды ответов сервера: 200, 400, 401 и 500.", DateTime.UtcNow, TestContext.Current.CancellationToken);
        var publication3 = await DI.CreatePublicationAsync(_db, null, DateTime.UtcNow.AddDays(-2), "Анонимные заметки", "Без привязки к автору.", null, TestContext.Current.CancellationToken);
        var publication4 = await DI.CreatePublicationAsync(_db, user2.Id, DateTime.UtcNow.AddDays(-3), "Тестирование", "Отклик бэкенда под нагрузкой.", DateTime.UtcNow.AddDays(-1), TestContext.Current.CancellationToken);
        var publication5 = await DI.CreatePublicationAsync(_db, user.Id, DateTime.UtcNow.AddDays(-4), "Заключение", "Финальная тестовая статья.", DateTime.UtcNow.AddDays(-2), TestContext.Current.CancellationToken);

        // 1. Act
        // Первый запрос без указания даты и курсора, лимит 2
        var cursorPaginatedList = await _publicationManager.GetCursorBasedPublicationsDtoAsync(date, lastId, limit, sortBy: sortBy, ct: TestContext.Current.CancellationToken);

        // 1. Assert
        // Элементы верно отсортированы, NextId, NextDate - корректны
        Assert.Equal(limit, cursorPaginatedList.Items.Count());

        Assert.Equal(publication.Id, cursorPaginatedList.Items.ElementAt(0).Id);
        Assert.Equal(publication2.Id, cursorPaginatedList.Items.ElementAt(1).Id);
        Assert.Equal(publication3.Id, cursorPaginatedList.NextId);
        Assert.Equal(publication3.CreatedAt.ToWithoutLastTick(), cursorPaginatedList.NextDate);

        Assert.Equal(limit, cursorPaginatedList.Limit);
        Assert.Equal(searchString, cursorPaginatedList.SearchString);
        Assert.Equal(sortBy, cursorPaginatedList.SortBy);
        Assert.True(cursorPaginatedList.HasMore);



        // 2. Arrange
        // Второй запрос с указанием даты и курсора, лимит 2
        date = cursorPaginatedList.NextDate;
        lastId = cursorPaginatedList.NextId;

        // 2. Act
        cursorPaginatedList = await _publicationManager.GetCursorBasedPublicationsDtoAsync(date, lastId, limit, sortBy: sortBy, ct: TestContext.Current.CancellationToken);

        // 2. Assert
        // Элементы верно отсортированы, NextId, NextDate - корректны
        Assert.Equal(limit, cursorPaginatedList.Items.Count());

        Assert.Equal(publication3.Id, cursorPaginatedList.Items.ElementAt(0).Id);
        Assert.Equal(publication4.Id, cursorPaginatedList.Items.ElementAt(1).Id);
        Assert.Equal(publication5.Id, cursorPaginatedList.NextId);
        Assert.Equal(publication5.CreatedAt.ToWithoutLastTick(), cursorPaginatedList.NextDate);

        Assert.Equal(limit, cursorPaginatedList.Limit);
        Assert.Equal(searchString, cursorPaginatedList.SearchString);
        Assert.Equal(sortBy, cursorPaginatedList.SortBy);
        Assert.True(cursorPaginatedList.HasMore);



        // 3. Arrange
        // Третий запрос с указанием даты и курсора, лимит 2
        date = cursorPaginatedList.NextDate;
        lastId = cursorPaginatedList.NextId;

        // 3. Act
        cursorPaginatedList = await _publicationManager.GetCursorBasedPublicationsDtoAsync(date, lastId, limit, sortBy: sortBy, ct: TestContext.Current.CancellationToken);

        // 3. Assert
        // Элемент один, NextId, NextDate - null, HasMore = false
        Assert.Single(cursorPaginatedList.Items);

        Assert.Equal(publication5.Id, cursorPaginatedList.Items.ElementAt(0).Id);
        Assert.Null(cursorPaginatedList.Items.ElementAtOrDefault(1));
        Assert.Null(cursorPaginatedList.NextId);
        Assert.Null(cursorPaginatedList.NextDate);

        Assert.Equal(limit, cursorPaginatedList.Limit);
        Assert.Equal(searchString, cursorPaginatedList.SearchString);
        Assert.Equal(sortBy, cursorPaginatedList.SortBy);
        Assert.False(cursorPaginatedList.HasMore);
    }

    // Пять статей, три последовательных запроса, пайплайн работает корректно (пагинация курсором)
    // Sort: date (от старой к новой)
    [Fact]
    public async Task CursorBasedPipeline_Sort_Date()
    {
        // Arrange
        DateTime? date = null;
        Guid? lastId = null;
        int limit = 2;
        string searchString = null;
        string sortBy = SortByVariables.date;

        // Добавляем пользователей в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var user2 = await DI.CreateUserAsync(_db, username: "test", email: "test", phoneNumber: "123", ct: TestContext.Current.CancellationToken);

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, DateTime.UtcNow, "Первые шаги", "Работа с REST API и отправка первых запросов.", null, TestContext.Current.CancellationToken);
        var publication2 = await DI.CreatePublicationAsync(_db, user2.Id, DateTime.UtcNow.AddDays(-1), "Ошибки", "Коды ответов сервера: 200, 400, 401 и 500.", DateTime.UtcNow, TestContext.Current.CancellationToken);
        var publication3 = await DI.CreatePublicationAsync(_db, null, DateTime.UtcNow.AddDays(-2), "Анонимные заметки", "Без привязки к автору.", null, TestContext.Current.CancellationToken);
        var publication4 = await DI.CreatePublicationAsync(_db, user2.Id, DateTime.UtcNow.AddDays(-3), "Тестирование", "Отклик бэкенда под нагрузкой.", DateTime.UtcNow.AddDays(-1), TestContext.Current.CancellationToken);
        var publication5 = await DI.CreatePublicationAsync(_db, user.Id, DateTime.UtcNow.AddDays(-4), "Заключение", "Финальная тестовая статья.", DateTime.UtcNow.AddDays(-2), TestContext.Current.CancellationToken);

        // 1. Act
        // Первый запрос без указания даты и курсора, лимит 2
        var cursorPaginatedList = await _publicationManager.GetCursorBasedPublicationsDtoAsync(date, lastId, limit, sortBy: sortBy, ct: TestContext.Current.CancellationToken);

        // 1. Assert
        // Элементы верно отсортированы, NextId, NextDate - корректны
        Assert.Equal(limit, cursorPaginatedList.Items.Count());

        Assert.Equal(publication5.Id, cursorPaginatedList.Items.ElementAt(0).Id);
        Assert.Equal(publication4.Id, cursorPaginatedList.Items.ElementAt(1).Id);
        Assert.Equal(publication3.Id, cursorPaginatedList.NextId);
        Assert.Equal(publication3.CreatedAt.ToWithoutLastTick(), cursorPaginatedList.NextDate);

        Assert.Equal(limit, cursorPaginatedList.Limit);
        Assert.Equal(searchString, cursorPaginatedList.SearchString);
        Assert.Equal(sortBy, cursorPaginatedList.SortBy);
        Assert.True(cursorPaginatedList.HasMore);



        // 2. Arrange
        // Второй запрос с указанием даты и курсора, лимит 2
        date = cursorPaginatedList.NextDate;
        lastId = cursorPaginatedList.NextId;

        // 2. Act
        cursorPaginatedList = await _publicationManager.GetCursorBasedPublicationsDtoAsync(date, lastId, limit, sortBy: sortBy, ct: TestContext.Current.CancellationToken);

        // 2. Assert
        // Элементы верно отсортированы, NextId, NextDate - корректны
        Assert.Equal(limit, cursorPaginatedList.Items.Count());

        Assert.Equal(publication3.Id, cursorPaginatedList.Items.ElementAt(0).Id);
        Assert.Equal(publication2.Id, cursorPaginatedList.Items.ElementAt(1).Id);
        Assert.Equal(publication.Id, cursorPaginatedList.NextId);
        Assert.Equal(publication.CreatedAt.ToWithoutLastTick(), cursorPaginatedList.NextDate);

        Assert.Equal(limit, cursorPaginatedList.Limit);
        Assert.Equal(searchString, cursorPaginatedList.SearchString);
        Assert.Equal(sortBy, cursorPaginatedList.SortBy);
        Assert.True(cursorPaginatedList.HasMore);



        // 3. Arrange
        // Третий запрос с указанием даты и курсора, лимит 2
        date = cursorPaginatedList.NextDate;
        lastId = cursorPaginatedList.NextId;

        // 3. Act
        cursorPaginatedList = await _publicationManager.GetCursorBasedPublicationsDtoAsync(date, lastId, limit, sortBy: sortBy, ct: TestContext.Current.CancellationToken);

        // 3. Assert
        // Элемент один, NextId, NextDate - null, HasMore = false
        Assert.Single(cursorPaginatedList.Items);

        Assert.Equal(publication.Id, cursorPaginatedList.Items.ElementAt(0).Id);
        Assert.Null(cursorPaginatedList.Items.ElementAtOrDefault(1));
        Assert.Null(cursorPaginatedList.NextId);
        Assert.Null(cursorPaginatedList.NextDate);

        Assert.Equal(limit, cursorPaginatedList.Limit);
        Assert.Equal(searchString, cursorPaginatedList.SearchString);
        Assert.Equal(sortBy, cursorPaginatedList.SortBy);
        Assert.False(cursorPaginatedList.HasMore);
    }

    // Пять статей из которых две совпадают с поиском. Отображаются результаты от старой к новой. Следующих элементов нет, т.к нет совпадений с поисков (всего 2 элемента)
    // Sort: date (от старой к новой)
    // +SearchString
    [Fact]
    public async Task CursorBasedPipeline_Sort_Date_WithSearchString()
    {
        // Arrange
        DateTime? date = null;
        Guid? lastId = null;
        int limit = 2;
        string searchString = "Первые шаги";
        string sortBy = SortByVariables.date;

        // Добавляем пользователей в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var user2 = await DI.CreateUserAsync(_db, username: "test", email: "test", phoneNumber: "123", ct: TestContext.Current.CancellationToken);

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, DateTime.UtcNow, "Первые шаги 2.0", "Работа с REST API и отправка первых запросов.", null, TestContext.Current.CancellationToken);
        var publication2 = await DI.CreatePublicationAsync(_db, user2.Id, DateTime.UtcNow.AddDays(-1), "Ошибки", "Коды ответов сервера: 200, 400, 401 и 500.", DateTime.UtcNow, TestContext.Current.CancellationToken);
        var publication3 = await DI.CreatePublicationAsync(_db, null, DateTime.UtcNow.AddDays(-2), "Анонимные заметки", "Без привязки к автору.", null, TestContext.Current.CancellationToken);
        var publication4 = await DI.CreatePublicationAsync(_db, user2.Id, DateTime.UtcNow.AddDays(-3), "Первые шаги", "Работа с REST API и отправка первых запросов.", DateTime.UtcNow.AddDays(-1), TestContext.Current.CancellationToken);
        var publication5 = await DI.CreatePublicationAsync(_db, user.Id, DateTime.UtcNow.AddDays(-4), "Заключение", "Финальная тестовая статья.", DateTime.UtcNow.AddDays(-2), TestContext.Current.CancellationToken);

        // от старой к новой
        // 1. "Первые шаги" (publication4) - т.к вышел раньше всех из совпадений по поиску (старый)
        // 2. "Первые шаги 2.0" (publication) - т.к вышел позже всех из совпадений по поиску (новый)
        // 3... нет. Т.к по поиску совпадений больше нет

        // Act
        // Первый запрос без указания даты и курсора, лимит 2
        var cursorPaginatedList = await _publicationManager.GetCursorBasedPublicationsDtoAsync(date, lastId, limit, searchString: searchString, sortBy: sortBy, ct: TestContext.Current.CancellationToken);

        // Assert
        // Элементы верно отсортированы, NextId, NextDate - корректны
        Assert.Equal(limit, cursorPaginatedList.Items.Count());

        Assert.Equal(publication4.Id, cursorPaginatedList.Items.ElementAt(0).Id);
        Assert.Equal(publication.Id, cursorPaginatedList.Items.ElementAt(1).Id);
        Assert.Null(cursorPaginatedList.NextId);
        Assert.Null(cursorPaginatedList.NextDate);

        Assert.Equal(limit, cursorPaginatedList.Limit);
        Assert.Equal(searchString, cursorPaginatedList.SearchString);
        Assert.Equal(sortBy, cursorPaginatedList.SortBy);
        Assert.False(cursorPaginatedList.HasMore);
    }

    // Пять статей из которых две совпадают с поиском. Отображаются результаты от новой к старой. Следующих элементов нет, т.к нет совпадений с поисков (всего 2 элемента)
    // Sort: date_desc (от новой к старой)
    // +SearchString
    [Fact]
    public async Task CursorBasedPipeline_Sort_DateDesc_WithSearchString()
    {
        // Arrange
        DateTime? date = null;
        Guid? lastId = null;
        int limit = 2;
        string searchString = "Первые шаги";
        string sortBy = SortByVariables.date_desc;

        // Добавляем пользователей в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var user2 = await DI.CreateUserAsync(_db, username: "test", email: "test", phoneNumber: "123", ct: TestContext.Current.CancellationToken);

        // Добавляем публикации в базу
        var publication = await DI.CreatePublicationAsync(_db, user.Id, DateTime.UtcNow, "Первые шаги 2.0", "Работа с REST API и отправка первых запросов.", null, TestContext.Current.CancellationToken);
        var publication2 = await DI.CreatePublicationAsync(_db, user2.Id, DateTime.UtcNow.AddDays(-1), "Ошибки", "Коды ответов сервера: 200, 400, 401 и 500.", DateTime.UtcNow, TestContext.Current.CancellationToken);
        var publication3 = await DI.CreatePublicationAsync(_db, null, DateTime.UtcNow.AddDays(-2), "Анонимные заметки", "Без привязки к автору.", null, TestContext.Current.CancellationToken);
        var publication4 = await DI.CreatePublicationAsync(_db, user2.Id, DateTime.UtcNow.AddDays(-3), "Первые шаги", "Работа с REST API и отправка первых запросов.", DateTime.UtcNow.AddDays(-1), TestContext.Current.CancellationToken);
        var publication5 = await DI.CreatePublicationAsync(_db, user.Id, DateTime.UtcNow.AddDays(-4), "Заключение", "Финальная тестовая статья.", DateTime.UtcNow.AddDays(-2), TestContext.Current.CancellationToken);

        // от старой к новой
        // 1. "Первые шаги 2.0" (publication) - т.к вышел позже всех из совпадений по поиску (новый)
        // 2. "Первые шаги" (publication4) - т.к вышел раньше всех из совпадений по поиску (старый)
        // 3... нет. Т.к по поиску совпадений больше нет

        // Act
        // Первый запрос без указания даты и курсора, лимит 2
        var cursorPaginatedList = await _publicationManager.GetCursorBasedPublicationsDtoAsync(date, lastId, limit, searchString: searchString, sortBy: sortBy, ct: TestContext.Current.CancellationToken);

        // Assert
        // Элементы верно отсортированы, NextId, NextDate - корректны
        Assert.Equal(limit, cursorPaginatedList.Items.Count());

        Assert.Equal(publication.Id, cursorPaginatedList.Items.ElementAt(0).Id);
        Assert.Equal(publication4.Id, cursorPaginatedList.Items.ElementAt(1).Id);
        Assert.Null(cursorPaginatedList.NextId);
        Assert.Null(cursorPaginatedList.NextDate);

        Assert.Equal(limit, cursorPaginatedList.Limit);
        Assert.Equal(searchString, cursorPaginatedList.SearchString);
        Assert.Equal(sortBy, cursorPaginatedList.SortBy);
        Assert.False(cursorPaginatedList.HasMore);
    }

    // Нет поддержки author_publications_count и author_publications_count_desc. Я отказался, чтобы не сложнять
}