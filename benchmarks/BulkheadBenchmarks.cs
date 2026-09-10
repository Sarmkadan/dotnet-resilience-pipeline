using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Benchmarks;

/// <summary>
/// Benchmarks for BulkheadPolicy performance
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class BulkheadBenchmarks
{
    private BulkheadPolicy _bulkheadPolicy;
    private const string PolicyName = "test-bulkhead";

    [GlobalSetup]
    /// <summary>
    /// Initializes the BulkheadPolicy with MaxParallelization=10 and MaxQueueLength=50 for each benchmark.
    /// </summary>
    public void Setup()
    {
        _bulkheadPolicy = new BulkheadPolicy(PolicyName)
        {
            MaxParallelization = 10,
            MaxQueueLength = 50
        };
    }

    [Benchmark]
    /// <summary>
    /// Measures the success rate of acquiring a slot when slots are available.
    /// </summary>
    public bool BulkheadPolicy_TryAcquireSlot_Available()
    {
        return _bulkheadPolicy.TryAcquireSlot();
    }

    [Benchmark]
    /// <summary>
    /// Measures the performance of releasing a previously acquired slot back to the bulkhead.
    /// </summary>
    public void BulkheadPolicy_ReleaseSlot()
    {
        _bulkheadPolicy.TryAcquireSlot();
        _bulkheadPolicy.ReleaseSlot();
    }

    [Benchmark]
    /// <summary>
    /// Measures the performance of recording a queue wait time measurement.
    /// </summary>
    public void BulkheadPolicy_RecordQueueWaitTime()
    {
        _bulkheadPolicy.RecordQueueWaitTime(100);
    }

    [Benchmark]
    /// <summary>
    /// Measures the utilization percentage when all available slots are acquired.
    /// </summary>
    public double BulkheadPolicy_GetUtilizationPercentage()
    {
        // Acquire all slots
        for (int i = 0; i < 10; i++)
        {
            _bulkheadPolicy.TryAcquireSlot();
        }
        return _bulkheadPolicy.GetUtilizationPercentage();
    }

    [Benchmark]
    /// <summary>
    /// Measures the percentage of queued requests when bulkhead is full and queue is partially filled.
    /// </summary>
    public double BulkheadPolicy_GetQueuedPercentage()
    {
        // Fill bulkhead and queue
        for (int i = 0; i < 10; i++)
        {
            _bulkheadPolicy.TryAcquireSlot();
        }
        for (int i = 0; i < 50; i++)
        {
            _bulkheadPolicy.TryAcquireSlot(); // These will be queued
        }
        return _bulkheadPolicy.GetQueuedPercentage();
    }

    [Benchmark]
    /// <summary>
    /// Measures the rejection percentage when bulkhead and queue are at capacity.
    /// </summary>
    public double BulkheadPolicy_GetRejectionPercentage()
    {
        // Fill bulkhead and queue completely
        for (int i = 0; i < 10; i++)
        {
            _bulkheadPolicy.TryAcquireSlot();
        }
        for (int i = 0; i < 50; i++)
        {
            _bulkheadPolicy.TryAcquireSlot(); // These will be queued
        }
        // Now try to exceed
        _bulkheadPolicy.TryAcquireSlot(); // This will be rejected
        return _bulkheadPolicy.GetRejectionPercentage();
    }

    [Benchmark]
    /// <summary>
    /// Measures the retrieval of the MaxParallelization property value.
    /// </summary>
    public int BulkheadPolicy_Get_MaxParallelization()
    {
        return _bulkheadPolicy.MaxParallelization;
    }

    [Benchmark]
    /// <summary>
    /// Measures the retrieval of the MaxQueueLength property value.
    /// </summary>
    public int BulkheadPolicy_Get_MaxQueueLength()
    {
        return _bulkheadPolicy.MaxQueueLength;
    }

    [Benchmark]
    /// <summary>
    /// Measures the retrieval of the ActiveExecutions property value.
    /// </summary>
    public int BulkheadPolicy_Get_ActiveExecutions()
    {
        return _bulkheadPolicy.ActiveExecutions;
    }
}