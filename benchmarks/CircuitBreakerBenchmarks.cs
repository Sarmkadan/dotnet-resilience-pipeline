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

    /// <summary>
    /// Initializes the circuit breaker policies for benchmarking.
    /// Sets up closed, half-open, and open state policies.
    /// </summary>
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

    /// <summary>
    /// Measures the performance of recording a successful call in the closed state.
    /// </summary>
    [Benchmark]
    public void CircuitBreaker_Closed_State()
    {
        _closedPolicy.RecordSuccess();
    }

    /// <summary>
    /// Measures the performance of recording a successful call in the half-open state.
    /// </summary>
    [Benchmark]
    public void CircuitBreaker_HalfOpen_State()
    {
        _halfOpenPolicy.RecordSuccess();
    }

    /// <summary>
    /// Measures the performance of attempting to reset the circuit breaker in the open state.
    /// </summary>
    [Benchmark]
    public void CircuitBreaker_Open_State()
    {
        _openPolicy.AttemptReset();
    }

    /// <summary>
    /// Measures the performance of recording a failed call in the closed state.
    /// </summary>
    [Benchmark]
    public void CircuitBreaker_Failure_Recording()
    {
        _closedPolicy.RecordFailure();
    }

    /// <summary>
    /// Measures the performance of transitioning the circuit breaker from closed to open state by recording multiple failures.
    /// </summary>
    [Benchmark]
    public void CircuitBreaker_State_Transition()
    {
        // Transition from closed to open
        for (int i = 0; i < 5; i++)
        {
            _closedPolicy.RecordFailure();
        }
    }

    /// <summary>
    /// Gets the current state of the circuit breaker in the closed state policy (should remain closed if no failures).
    /// </summary>
    [Benchmark]
    public CircuitBreakerPolicy.CircuitState CircuitBreaker_Get_CurrentState()
    {
        return _closedPolicy.CurrentState;
    }

    /// <summary>
    /// Gets the number of times the circuit breaker has tripped (transitioned to open) in the closed state policy.
    /// </summary>
    [Benchmark]
    public long CircuitBreaker_Get_CircuitBreakerTrips()
    {
        return _closedPolicy.CircuitBreakerTrips;
    }
}