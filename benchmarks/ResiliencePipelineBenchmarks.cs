using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotNetResiliencePipeline.Domain;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;

namespace DotNetResiliencePipeline.Benchmarks;

/// <summary>
/// Benchmarks for complete ResiliencyPipelineService performance
/// Measures end-to-end pipeline execution with all policy types combined
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ResiliencePipelineBenchmarks
{
    private ResiliencyPipelineService _pipelineService;
    private CircuitBreakerPolicy _circuitBreakerPolicy;
    private RetryPolicy _retryPolicy;
    private TimeoutPolicy _timeoutPolicy;
    private BulkheadPolicy _bulkheadPolicy;
    private FallbackPolicy _fallbackPolicy;
    private const string PipelineName = "full-pipeline";

    [GlobalSetup]
    public void Setup()
    {
        // Create individual policies
        _circuitBreakerPolicy = new CircuitBreakerPolicy("cb-pipeline")
        {
            FailureThreshold = 5,
            OpenDuration = TimeSpan.FromSeconds(30)
        };

        _retryPolicy = new RetryPolicy("retry-pipeline")
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromMilliseconds(100),
            Strategy = RetryPolicy.BackoffStrategy.Exponential,
            BackoffMultiplier = 2.0,
            MaxDelay = TimeSpan.FromSeconds(30)
        };

        _timeoutPolicy = new TimeoutPolicy("timeout-pipeline")
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        _bulkheadPolicy = new BulkheadPolicy("bulkhead-pipeline")
        {
            MaxParallelization = 10,
            MaxQueueLength = 50
        };

        _fallbackPolicy = new FallbackPolicy("fallback-pipeline")
        {
            FallbackOnAnyException = true,
            FallbackTimeout = TimeSpan.FromSeconds(5)
        };

        // Create pipeline service
        _pipelineService = new ResiliencyPipelineService();
    }

    [Benchmark]
    public async Task ResiliencePipeline_Execute_Successful_Operation()
    {
        await _pipelineService.ExecuteAsync(
            async ct => await Task.FromResult(42),
            cancellationToken: CancellationToken.None
        );
    }

    [Benchmark]
    public async Task ResiliencePipeline_Execute_With_CircuitBreaker()
    {
        await _pipelineService.ExecuteAsync(
            async ct => await Task.FromResult("success"),
            circuitBreaker: _circuitBreakerPolicy,
            cancellationToken: CancellationToken.None
        );
    }

    [Benchmark]
    public async Task ResiliencePipeline_Execute_With_Retry()
    {
        await _pipelineService.ExecuteAsync(
            async ct => await Task.FromResult(true),
            retry: _retryPolicy,
            cancellationToken: CancellationToken.None
        );
    }

    [Benchmark]
    public async Task ResiliencePipeline_Execute_With_Timeout()
    {
        await _pipelineService.ExecuteAsync(
            async ct => await Task.FromResult(123.45m),
            timeout: _timeoutPolicy,
            cancellationToken: CancellationToken.None
        );
    }

    [Benchmark]
    public async Task ResiliencePipeline_Execute_With_Bulkhead()
    {
        await _pipelineService.ExecuteAsync(
            async ct => await Task.FromResult("result"),
            bulkhead: _bulkheadPolicy,
            cancellationToken: CancellationToken.None
        );
    }

    [Benchmark]
    public async Task ResiliencePipeline_Execute_With_Fallback()
    {
        await _pipelineService.ExecuteAsync(
            async ct => throw new InvalidOperationException("Primary operation failed"),
            fallback: _fallbackPolicy,
            cancellationToken: CancellationToken.None
        );
    }

    [Benchmark]
    public async Task ResiliencePipeline_Execute_Full_Pipeline()
    {
        await _pipelineService.ExecuteAsync(
            async ct => await Task.FromResult("final result"),
            circuitBreaker: _circuitBreakerPolicy,
            retry: _retryPolicy,
            timeout: _timeoutPolicy,
            bulkhead: _bulkheadPolicy,
            fallback: _fallbackPolicy,
            cancellationToken: CancellationToken.None
        );
    }

    [Benchmark]
    public PipelineStatistics ResiliencePipeline_Get_Statistics()
    {
        return _pipelineService.GetStatistics();
    }

    [Benchmark]
    public async Task ResiliencePipeline_Execute_Multiple_Operations_Parallel()
    {
        var tasks = new List<Task<PolicyResult<int>>>();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(_pipelineService.ExecuteAsync(
                async ct => await Task.FromResult(i),
                cancellationToken: CancellationToken.None
            ));
        }
        await Task.WhenAll(tasks);
    }
}