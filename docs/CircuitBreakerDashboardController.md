# CircuitBreakerDashboardController

Provides a centralized API endpoint for monitoring and managing circuit breaker instances within the resilience pipeline. It exposes real-time aggregate health statistics, individual breaker statuses, and administrative reset capabilities. The controller serves as the primary interface for dashboards and operational tooling to observe and interact with circuit breaker state across the system.

## API

### Public Members

#### `public CircuitBreakerDashboardController`
Constructor. Initializes a new instance of the controller with the necessary dependencies to query and command circuit breaker instances managed by the resilience pipeline.

#### `public Task<ApiResponse<CircuitBreakerDashboardDto>> GetDashboardAsync`
Retrieves a complete snapshot of all tracked circuit breakers along with aggregate health metrics. Returns an `ApiResponse` wrapping a `CircuitBreakerDashboardDto` that contains the overall health summary and the list of individual breaker statuses. This method does not throw expected exceptions; failures are communicated through the `ApiResponse` envelope.

#### `public Task<ApiResponse<CircuitBreakerStatusDto>> GetBreakerStatusAsync`
Queries the current status of a specific circuit breaker identified by its policy identifier. The policy ID must be supplied as a parameter. Returns an `ApiResponse` wrapping a `CircuitBreakerStatusDto` with the breaker’s state, failure counts, and timing information. Throws an `ArgumentException` when the provided policy ID is null or whitespace. Returns a faulted `ApiResponse` if the specified breaker is not found.

#### `public Task<ApiResponse<CircuitBreakerStatusDto>> ResetBreakerAsync`
Forces an immediate reset of a specific circuit breaker, transitioning it to the Closed state and clearing its failure history. The policy ID must be supplied as a parameter. Returns an `ApiResponse` wrapping the updated `CircuitBreakerStatusDto` reflecting the post-reset state. Throws an `ArgumentException` when the provided policy ID is null or whitespace. Returns a faulted `ApiResponse` if the specified breaker does not exist or cannot be reset.

#### `public Task<ApiResponse<List<CircuitBreakerStatusDto>>> GetOpenBreakersAsync`
Retrieves the status of all circuit breakers currently in the Open state. Returns an `ApiResponse` wrapping a list of `CircuitBreakerStatusDto` objects, which may be empty if no breakers are open. This method does not throw expected exceptions; failures are communicated through the `ApiResponse` envelope.

#### `public DateTime GeneratedAt`
The UTC timestamp at which the current dashboard snapshot was generated. This value is populated when `GetDashboardAsync` completes successfully and reflects the moment the aggregate data was computed.

#### `public int TotalBreakers`
The total number of circuit breaker instances being tracked by the dashboard at the time of snapshot generation.

#### `public int ClosedCount`
The number of circuit breakers currently in the Closed state, allowing normal operation.

#### `public int OpenCount`
The number of circuit breakers currently in the Open state, rejecting requests due to exceeded failure thresholds.

#### `public int HalfOpenCount`
The number of circuit breakers currently in the HalfOpen state, permitting a limited number of trial requests to test recovery.

#### `public long TotalTrips`
The cumulative number of times any tracked circuit breaker has transitioned to the Open state since tracking began.

#### `public string OverallHealth`
A string label summarizing the aggregate health of all circuit breakers. Typical values include "Healthy", "Degraded", or "Critical", derived from the proportion of open breakers relative to the total.

#### `public List<CircuitBreakerStatusDto> Breakers`
The collection of individual circuit breaker status entries included in the dashboard snapshot. Each entry corresponds to a distinct policy instance.

#### `public string PolicyId`
The unique identifier of the resilience policy associated with a specific circuit breaker instance.

#### `public string Name`
The human-readable name assigned to the circuit breaker policy for identification in logs and dashboards.

#### `public string State`
The current state of the circuit breaker. Returns one of three string values: `"Closed"`, `"Open"`, or `"HalfOpen"`.

#### `public int ConsecutiveFailures`
The current count of consecutive operation failures recorded by the circuit breaker while in the Closed or HalfOpen states. This counter resets upon a successful operation or a state transition.

#### `public int FailureThreshold`
The configured threshold of consecutive failures that triggers a transition from Closed to Open.

#### `public long TripCount`
The total number of times this specific circuit breaker instance has tripped into the Open state over its lifetime.

#### `public double? SecondsUntilHalfOpen`
When the breaker is in the Open state, this value indicates the remaining seconds before it automatically transitions to HalfOpen. Returns `null` when the breaker is not in the Open state or when the transition timing is not available.

## Usage

### Example 1: Monitoring Overall System Health

```csharp
// Retrieve the full dashboard and evaluate overall health
var dashboardResponse = await controller.GetDashboardAsync();

if (dashboardResponse.IsSuccess)
{
    var dashboard = dashboardResponse.Data;
    Console.WriteLine($"Health: {dashboard.OverallHealth}");
    Console.WriteLine($"Open: {dashboard.OpenCount} / {dashboard.TotalBreakers}");
    Console.WriteLine($"Total trips: {dashboard.TotalTrips}");
    Console.WriteLine($"Snapshot generated at: {dashboard.GeneratedAt:u}");

    foreach (var breaker in dashboard.Breakers)
    {
        Console.WriteLine($"  {breaker.Name} ({breaker.PolicyId}): {breaker.State}");
    }
}
else
{
    Console.WriteLine($"Dashboard retrieval failed: {dashboardResponse.ErrorMessage}");
}
```

### Example 2: Resetting a Tripped Breaker After Remediation

```csharp
// Identify and reset a specific breaker that is stuck open
var openBreakersResponse = await controller.GetOpenBreakersAsync();

if (openBreakersResponse.IsSuccess && openBreakersResponse.Data.Any())
{
    foreach (var breaker in openBreakersResponse.Data)
    {
        Console.WriteLine($"Resetting {breaker.Name} ({breaker.PolicyId})...");
        var resetResponse = await controller.ResetBreakerAsync(breaker.PolicyId);

        if (resetResponse.IsSuccess)
        {
            Console.WriteLine($"  New state: {resetResponse.Data.State}");
            Console.WriteLine($"  Failures cleared: {resetResponse.Data.ConsecutiveFailures}");
        }
        else
        {
            Console.WriteLine($"  Reset failed: {resetResponse.ErrorMessage}");
        }
    }
}
else
{
    Console.WriteLine("No open breakers found.");
}
```

## Notes

- All asynchronous methods return `ApiResponse<T>` envelopes rather than throwing exceptions for operational failures. Callers must inspect the `IsSuccess` property before accessing the `Data` payload.
- `GetBreakerStatusAsync` and `ResetBreakerAsync` throw `ArgumentException` synchronously for null or whitespace policy IDs. This validation occurs before any asynchronous work begins.
- The `SecondsUntilHalfOpen` property is nullable and only meaningful when `State` equals `"Open"`. Consumers should null-check this value before performing calculations or displaying countdowns.
- The `OverallHealth` string is a computed label based on the ratio of open breakers. Its exact threshold values are implementation-defined and may change across versions; avoid hardcoding comparisons against specific strings in business logic.
- The `Breakers` list and aggregate counters (`ClosedCount`, `OpenCount`, `HalfOpenCount`, `TotalTrips`) represent a point-in-time snapshot identified by `GeneratedAt`. Concurrent breaker state changes occurring after snapshot generation are not reflected until the next `GetDashboardAsync` call.
- Thread safety: The controller itself is designed to be used concurrently from multiple request threads. The underlying resilience pipeline state is inherently mutable; reads via the dashboard methods are atomic snapshots but do not lock the pipeline, meaning individual breaker statuses may change between successive calls within the same logical operation.
