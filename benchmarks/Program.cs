using BenchmarkDotNet.Running;
using DotNetResiliencePipeline.Benchmarks;

namespace DotNetResiliencePipeline.Benchmarks;

/// <summary>
/// Entry point for the benchmark application.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point for the benchmark application.
    /// </summary>
    /// <param name="args">
    /// Command-line arguments passed to the benchmark runner.
    /// The arguments are passed directly to BenchmarkRunner.Run methods.
    /// </param>
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<CircuitBreakerBenchmarks>();
        BenchmarkRunner.Run<RetryBenchmarks>();
        BenchmarkRunner.Run<TimeoutBenchmarks>();
        BenchmarkRunner.Run<BulkheadBenchmarks>();
        BenchmarkRunner.Run<ResiliencePipelineBenchmarks>();
    }
}