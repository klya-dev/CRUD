using BenchmarkDotNet.Running;

namespace Benchmarking;

public class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        BenchmarkRunner.Run<TestBenchmark>();
    }
}