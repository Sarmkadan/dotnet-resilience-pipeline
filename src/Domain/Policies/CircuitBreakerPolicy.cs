#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Circuit breaker pattern implementation that prevents cascading failures.
/// States: Closed (normal) -> Open (fail-fast) -> Half-Open (testing) -> Closed
/// </summary>
public class CircuitBreakerPolicy : ResiliencyPolicy
{
    public enum CircuitState
    {
        Closed,      // Normal operation
        Open,        // Rejecting requests
        HalfOpen     // Testing recovery
    }

    private CircuitState _state = CircuitState.Closed;
    private DateTime _lastStateChange = DateTime.UtcNow;
    private int _consecutiveFailures = 0;

    /// <summary>
    /// Number of consecutive failures before opening the circuit.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Duration the circuit remains open before transitioning to half-open.
    /// </summary>
    public TimeSpan OpenDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Number of successful executions in half-open state to close the circuit.
    /// </summary>
    public int SuccessThresholdInHalfOpen { get; set; } = 3;

    /// <summary>
    /// Current state of the circuit.
    /// </summary>
    public CircuitState CurrentState
    {
        get => _state;
        private set
        {
            if (_state != value)
            {
                _state = value;
                _lastStateChange = DateTime.UtcNow;
                Metadata["LastStateChange"] = _lastStateChange;
            }
        }
    }

    /// <summary>
    /// Number of consecutive failures recorded.
    /// </summary>
    public int ConsecutiveFailures
    {
        get => _consecutiveFailures;
        private set => _consecutiveFailures = value;
    }

    /// <summary>
    /// Time remaining until half-open state (if circuit is open).
    /// </summary>
    public TimeSpan? TimeUntilHalfOpen
    {
        get
        {
            if (CurrentState != CircuitState.Open)
                return null;

            var elapsed = DateTime.UtcNow - _lastStateChange;
            var remaining = OpenDuration - elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    public CircuitBreakerPolicy(string name) : base(name)
    {
        Metadata["CircuitState"] = CurrentState;
    }

    /// <summary>
    /// Records a successful execution and potentially transitions state.
    /// </summary>
    public override void RecordSuccess()
    {
        base.RecordSuccess();

        if (CurrentState == CircuitState.HalfOpen)
        {
            int successfulInHalfOpen = (int?)Metadata.GetValueOrDefault("SuccessfulInHalfOpen") ?? 0;
            successfulInHalfOpen++;
            Metadata["SuccessfulInHalfOpen"] = successfulInHalfOpen;

            if (successfulInHalfOpen >= SuccessThresholdInHalfOpen)
            {
                CloseCircuit();
            }
        }
        else if (CurrentState == CircuitState.Closed)
        {
            ConsecutiveFailures = 0;
        }
    }

    /// <summary>
    /// Records a failed execution and potentially transitions to open state.
    /// </summary>
    public override void RecordFailure()
    {
        base.RecordFailure();

        if (CurrentState == CircuitState.Closed)
        {
            ConsecutiveFailures++;
            if (ConsecutiveFailures >= FailureThreshold)
            {
                OpenCircuit();
            }
        }
        else if (CurrentState == CircuitState.HalfOpen)
        {
            OpenCircuit();
        }
    }

    /// <summary>
    /// Checks if the circuit should transition to half-open state.
    /// </summary>
    public void AttemptReset()
    {
        if (CurrentState == CircuitState.Open &&
            DateTime.UtcNow - _lastStateChange >= OpenDuration)
        {
            TransitionToHalfOpen();
        }
    }

    /// <summary>
    /// Opens the circuit, rejecting further requests.
    /// </summary>
    private void OpenCircuit()
    {
        CurrentState = CircuitState.Open;
        Metadata["CircuitState"] = CurrentState;
        Metadata["OpenedAt"] = DateTime.UtcNow;
    }

    /// <summary>
    /// Transitions to half-open state to test if the system recovered.
    /// </summary>
    private void TransitionToHalfOpen()
    {
        CurrentState = CircuitState.HalfOpen;
        Metadata["CircuitState"] = CurrentState;
        Metadata["SuccessfulInHalfOpen"] = 0;
    }

    /// <summary>
    /// Closes the circuit, resuming normal operation.
    /// </summary>
    private void CloseCircuit()
    {
        CurrentState = CircuitState.Closed;
        Metadata["CircuitState"] = CurrentState;
        ConsecutiveFailures = 0;
        Metadata["ClosedAt"] = DateTime.UtcNow;
    }

    /// <summary>
    /// Manually resets the circuit to closed state.
    /// </summary>
    public void ManualReset()
    {
        CloseCircuit();
        ResetStatistics();
    }

    /// <summary>
    /// Gets a detailed snapshot of the circuit breaker state.
    /// </summary>
    public override PolicySnapshot GetSnapshot()
    {
        var baseSnapshot = base.GetSnapshot();
        baseSnapshot.Metadata = new Dictionary<string, object>
        {
            { "CircuitState", CurrentState },
            { "ConsecutiveFailures", ConsecutiveFailures },
            { "FailureThreshold", FailureThreshold },
            { "TimeUntilHalfOpen", TimeUntilHalfOpen?.TotalSeconds ?? -1 }
        };
        return baseSnapshot;
    }
}
