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
    public void Setup()
    {
        _bulkheadPolicy = new BulkheadPolicy(PolicyName)
        {
            MaxParallelization = 10,
            MaxQueueLength = 50
        };
    }

    [Benchmark]
    public bool BulkheadPolicy_TryAcquireSlot_Available()
    {
        return _bulkheadPolicy.TryAcquireSlot();
    }

    [Benchmark]
    public void BulkheadPolicy_ReleaseSlot()
    {
        _bulkheadPolicy.TryAcquireSlot();
        _bulkheadPolicy.ReleaseSlot();
    }

    [Benchmark]
    public void BulkheadPolicy_RecordQueueWaitTime()
    {
        _bulkheadPolicy.RecordQueueWaitTime(100);
    }

    [Benchmark]
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
    public int BulkheadPolicy_Get_MaxParallelization()
    {
        return _bulkheadPolicy.MaxParallelization;
    }

    [Benchmark]
    public int BulkheadPolicy_Get_MaxQueueLength()
    {
        return _bulkheadPolicy.MaxQueueLength;
    }

    [Benchmark]
    public int BulkheadPolicy_Get_ActiveExecutions()
    {
        return _bulkheadPolicy.ActiveExecutions;
    }
}