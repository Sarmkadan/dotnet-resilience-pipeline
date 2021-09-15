using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Benchmarks;

/// <summary>
/// Benchmarks for CircuitBreakerPolicy performance
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CircuitBreakerBenchmarks
{
    private CircuitBreakerPolicy _closedPolicy;
    private CircuitBreakerPolicy _halfOpenPolicy;
    private CircuitBreakerPolicy _openPolicy;
    private const string PolicyName = "test-circuit-breaker";

    [GlobalSetup]
    public void Setup()
    {
        _closedPolicy = new CircuitBreakerPolicy(PolicyName)
        {
            FailureThreshold = 5,
            OpenDuration = TimeSpan.FromSeconds(30)
        };

        _halfOpenPolicy = new CircuitBreakerPolicy(PolicyName + "-halfopen")
        {
            FailureThreshold = 5,
            OpenDuration = TimeSpan.FromSeconds(30)
        };
        // Manually set to half-open state
        _halfOpenPolicy.GetType().GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_halfOpenPolicy, CircuitBreakerPolicy.CircuitState.HalfOpen);
        _halfOpenPolicy.GetType().GetField("_successThresholdInHalfOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_halfOpenPolicy, 3);

        _openPolicy = new CircuitBreakerPolicy(PolicyName + "-open")
        {
            FailureThreshold = 5,
            OpenDuration = TimeSpan.FromSeconds(30)
        };
        // Manually set to open state
        _openPolicy.GetType().GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_openPolicy, CircuitBreakerPolicy.CircuitState.Open);
        _openPolicy.GetType().GetField("_circuitBreakerTrips", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_openPolicy, 1L);
    }

    [Benchmark]
    public void CircuitBreaker_Closed_State()
    {
        _closedPolicy.RecordSuccess();
    }

    [Benchmark]
    public void CircuitBreaker_HalfOpen_State()
    {
        _halfOpenPolicy.RecordSuccess();
    }

    [Benchmark]
    public void CircuitBreaker_Open_State()
    {
        _openPolicy.AttemptReset();
    }

    [Benchmark]
    public void CircuitBreaker_Failure_Recording()
    {
        _closedPolicy.RecordFailure();
    }

    [Benchmark]
    public void CircuitBreaker_State_Transition()
    {
        // Transition from closed to open
        for (int i = 0; i < 5; i++)
        {
            _closedPolicy.RecordFailure();
        }
    }

    [Benchmark]
    public CircuitBreakerPolicy.CircuitState CircuitBreaker_Get_CurrentState()
    {
        return _closedPolicy.CurrentState;
    }

    [Benchmark]
    public long CircuitBreaker_Get_CircuitBreakerTrips()
    {
        return _closedPolicy.CircuitBreakerTrips;
    }
}