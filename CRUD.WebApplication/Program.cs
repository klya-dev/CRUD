// –аботать запросы к API будут даже с простого файлика index.html с JS, но дл€ правдоподобности и дл€ теста CORS мне нужен URL

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/url", (IConfiguration configuration) =>
{
    var apiUrl = configuration["ApiBaseUrl"];

    // ¬озвращаем в формате JSON
    return Results.Ok(apiUrl);
});

app.Run();