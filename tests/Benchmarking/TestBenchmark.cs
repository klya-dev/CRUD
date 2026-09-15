using AngleSharp.Io;
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
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace Benchmarking;

// Почему-то если нажимать в VS на пуск, то программа тупо пишет "завершила работу без ошибок"
// Поэтому я лезу в папку с релизом и от туда через cmd вызываю экзешник

sealed class Config : ManualConfig
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
    private static readonly List<int> list = Enumerable.Range(1, 10000000).ToList();

    public TestBenchmark()
    {
        //using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.SetMinimumLevel(LogLevel.Trace).AddConsole());
        //_logger = loggerFactory.CreateLogger<TestBenchmark>();
    }

    [Benchmark(Baseline = true)]
    public void Method1()
    {
        var a = list.Last();
    }

    [Benchmark]
    public void Method2()
    {
        var a = list[^1];
    }

    /*
        | Method  | Mean      | Ratio          | Allocated |
        |-------- |----------:|-------------:|-------:
        | Method1 |  1.612 ns |     baseline |     - |
        | Method2 | 1.408 ns | 1.15 faster |     - |
    */
}