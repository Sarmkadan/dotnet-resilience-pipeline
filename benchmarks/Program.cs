using BenchmarkDotNet.Running;
using DotNetResiliencePipeline.Benchmarks;

namespace DotNetResiliencePipeline.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<CircuitBreakerBenchmarks>();
        BenchmarkRunner.Run<RetryBenchmarks>();
        BenchmarkRunner.Run<TimeoutBenchmarks>();
        BenchmarkRunner.Run<BulkheadBenchmarks>();
        BenchmarkRunner.Run<ResiliencePipelineBenchmarks>();
    }
}