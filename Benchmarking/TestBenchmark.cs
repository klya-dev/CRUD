using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using CRUD.DataAccess.Data;
using CRUD.Models;
using CRUD.Models.Domains;
using CRUD.Models.Dtos;
using CRUD.Models.Dtos.Publication;
using CRUD.Services;
using CRUD.Services.Interfaces;
using CRUD.Tests;
using CRUD.Tests.Helpers;
using CRUD.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace Benchmarking;

// Почему-то если нажимать в VS на пуск, то программа тупо пишет "завершила работу без ошибок"
// Поэтому я лезу в папку с релизом и от туда через cmd вызываю экзешник

class Config : ManualConfig
{
    public Config()
    {
        // https://davecallan.com/how-to-set-the-ratio-column-style-in-benchmarkdotnet-results/
        SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend);
    }
}

[MemoryDiagnoser]
[Config(typeof(Config))]
[HideColumns("Error", "StdDev", "Median", "RatioSD", "Alloc Ratio")]
//[MinIterationCount(10)]
//[MaxIterationCount(20)]
//[InvocationCount(10)]
public partial class TestBenchmark
{
    private static readonly ApplicationDbContext db = DbContextGenerator.GenerateDbContextTest(false);
    //private static ILogger<TestBenchmark> _logger = new LoggerFactory().CreateLogger<TestBenchmark>();
    //private static readonly Stream stream = new FileStream(@"C:\Users\Admin\Desktop\1.png", FileMode.Open, FileAccess.Read);
    //private static readonly TokenManager tokenManager = new TokenManager();

    public TestBenchmark()
    {
        //using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.SetMinimumLevel(LogLevel.Trace).AddConsole());
        //_logger = loggerFactory.CreateLogger<TestBenchmark>();
    }

    [Benchmark(Baseline = true)]
    public void Method1()
    {
        string fileName = "log-20250822.txt";

        var removeStart = fileName.Remove(0, fileName.IndexOf('-') + 1); // Было log-20250822.txt, стало 20250822.txt
        var sanitizedFileName = removeStart.Remove(removeStart.IndexOf('.')); // Было 20250822.txt, стало 20250822
        var sanitizedFileDate = sanitizedFileName.Insert(4, ".").Insert(7, "."); // Было 20250822, стало 2025.08.22
        if (DateTime.TryParseExact(sanitizedFileDate, "yyyy.MM.dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fileDate))
        {
            if (fileDate.Date >= DateTime.Now.Date)
                return;
        }
    }

    [Benchmark]
    public void Method2()
    {
        string fileName = "log-20250822.txt";

        var startIndexOf = fileName.IndexOf('-');
        var endIndexOf = fileName.LastIndexOf('.'); // LastIndexOf, если я вдруг поменяю формат, где будут ещё точки
        var sanitizedFileName = fileName.Substring(startIndexOf + 1, fileName.Length - endIndexOf + startIndexOf + 1); // Было log-20250822.txt, стало 20250822
        var sanitizedFileDate = sanitizedFileName.Insert(4, ".").Insert(7, "."); // Было 20250822, стало 2025.08.22
        if (DateTime.TryParseExact(sanitizedFileDate, "yyyy.MM.dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fileDate))
        {
            if (fileDate.Date >= DateTime.Now.Date)
                return;
        }
    }

    [Benchmark]
    public void Method3()
    {
        string fileName = "log-20250822.txt";

        var sanitizedFileName = fileName.Substring(4, 8); // Было log-20250822.txt, стало 20250822
        var sanitizedFileDate = sanitizedFileName.Insert(4, ".").Insert(7, "."); // Было 20250822, стало 2025.08.22
        if (DateTime.TryParseExact(sanitizedFileDate, "yyyy.MM.dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fileDate))
        {
            if (fileDate.Date >= DateTime.Now.Date)
                return;
        }
    }

    /*
     | Method  | Mean      | Ratio        | Gen0   | Allocated |
     |-------- |----------:|-------------:|-------:|----------:|
     | Method1 | 111.07 ns |     baseline | 0.0105 |     176 B |
     | Method2 |  99.16 ns | 1.12x faster | 0.0076 |     128 B |
     | Method3 |  96.01 ns | 1.16x faster | 0.0076 |     128 B |
    */
}