#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Domain.Policies;

public override string ToString() => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}";

    /// <summary>
/// Circuit breaker pattern implementation that prevents cascading failures.
/// States: Closed (normal) -> Open (fail-fast) -> Half-Open (testing) -> Closed
/// </summary>
public override string ToString() => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}";

    /// <summary>
/// The <see cref="CircuitBreakerPolicy"/> class implements the circuit breaker pattern to prevent cascading failures.
/// It monitors failures and opens the circuit to reject requests when thresholds are exceeded,
/// then allows recovery testing in a half-open state before closing the circuit.
/// </summary>
/// <seealso cref="ResiliencyPolicy"/>
public sealed class CircuitBreakerPolicy : ResiliencyPolicy
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
    private long _circuitBreakerTrips = 0;

    public override string ToString() => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}";

    /// <summary>
    /// Number of consecutive failures before opening the circuit.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    public override string ToString() => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}";

    /// <summary>
    /// Duration the circuit remains open before transitioning to half-open.
    /// </summary>
    public TimeSpan OpenDuration { get; set; } = TimeSpan.FromSeconds(30);

    public override string ToString() => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}";

    /// <summary>
    /// Number of successful executions in half-open state to close the circuit.
    /// </summary>
    public int SuccessThresholdInHalfOpen
    {
        get => _successThresholdInHalfOpen;
        set => _successThresholdInHalfOpen = value <= 0 ? 1 : value; // Ensure it's at least 1
    }
    private int _successThresholdInHalfOpen = 3; // backing field

    public override string ToString() => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}";

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

    public override string ToString() => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}";

    /// <summary>
    /// Total number of times the circuit has tripped to the Open state.
    /// </summary>
    public long CircuitBreakerTrips => _circuitBreakerTrips;

    public override string ToString() => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}";

    /// <summary>
    /// Number of consecutive failures recorded.
    /// </summary>
    public int ConsecutiveFailures
    {
        get => _consecutiveFailures;
        private set => _consecutiveFailures = value;
    }

    public override string ToString() => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}";

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

    public override string ToString() => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}";

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

    public override string ToString() => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}";

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

    public override string ToString() => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}";

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

    public override string ToString() => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}";

    /// <summary>
    /// Opens the circuit, rejecting further requests.
    /// </summary>
    private void OpenCircuit()
    {
        _circuitBreakerTrips++;
        CurrentState = CircuitState.Open;
        Metadata["CircuitState"] = CurrentState;
        Metadata["OpenedAt"] = DateTime.UtcNow;
    }

    public override string ToString() => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}";

    /// <summary>
    /// Transitions to half-open state to test if the system recovered.
    /// </summary>
    private void TransitionToHalfOpen()
    {
        CurrentState = CircuitState.HalfOpen;
        Metadata["CircuitState"] = CurrentState;
        Metadata["SuccessfulInHalfOpen"] = 0;
    }

    public override string ToString() => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}";

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

    public override string ToString() => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}";

    /// <summary>
    /// Manually resets the circuit to closed state.
    /// </summary>
    public void ManualReset()
    {
        CloseCircuit();
        ResetStatistics();
    }

    public override string ToString() => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}"; => $"CircuitBreakerPolicy {{ FailureThreshold = {FailureThreshold}, OpenDuration = {OpenDuration} }}";

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
            { "CircuitBreakerTrips", CircuitBreakerTrips },
            { "TimeUntilHalfOpen", TimeUntilHalfOpen?.TotalSeconds ?? -1 }
        };
        return baseSnapshot;
    }
}
