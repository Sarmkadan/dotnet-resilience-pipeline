#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;

namespace DotNetResiliencePipeline.Api.Controllers;

/// <summary>
/// REST API controller providing a real-time dashboard for all circuit breakers in the pipeline.
/// Surfaces state, trip history, and per-breaker health indicators.
/// </summary>
public sealed class CircuitBreakerDashboardController
{
    private readonly ResiliencyPipelineService _pipelineService;
    private readonly CircuitBreakerService _circuitBreakerService;

    /// <summary>
    /// Initializes the dashboard controller.
    /// </summary>
    public CircuitBreakerDashboardController(
        ResiliencyPipelineService pipelineService,
        CircuitBreakerService circuitBreakerService)
    {
        _pipelineService = pipelineService ?? throw new ArgumentNullException(nameof(pipelineService));
        _circuitBreakerService = circuitBreakerService ?? throw new ArgumentNullException(nameof(circuitBreakerService));
    }

    /// <summary>
    /// GET /api/dashboard/circuit-breakers — Returns a summary dashboard for all circuit breakers.
    /// </summary>
    public Task<ApiResponse<CircuitBreakerDashboardDto>> GetDashboardAsync()
    {
        try
        {
            var breakers = GetAllCircuitBreakers();

            var dto = new CircuitBreakerDashboardDto
            {
                GeneratedAt = DateTime.UtcNow,
                TotalBreakers = breakers.Count,
                ClosedCount = breakers.Count(b => b.State == "Closed"),
                OpenCount = breakers.Count(b => b.State == "Open"),
                HalfOpenCount = breakers.Count(b => b.State == "HalfOpen"),
                TotalTrips = breakers.Sum(b => b.TripCount),
                Breakers = breakers,
                OverallHealth = ComputeOverallHealth(breakers)
            };

            return Task.FromResult(new ApiResponse<CircuitBreakerDashboardDto> { Success = true, Data = dto });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ApiResponse<CircuitBreakerDashboardDto> { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/dashboard/circuit-breakers/{name} — Returns the status of a single circuit breaker by policy name.
    /// </summary>
    public Task<ApiResponse<CircuitBreakerStatusDto>> GetBreakerStatusAsync(string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                return Task.FromResult(new ApiResponse<CircuitBreakerStatusDto> { Success = false, Message = "Name is required" });

            var policy = _pipelineService.GetPolicyByName(name) as CircuitBreakerPolicy;
            if (policy is null)
                return Task.FromResult(new ApiResponse<CircuitBreakerStatusDto> { Success = false, Message = $"Circuit breaker '{name}' not found" });

            return Task.FromResult(new ApiResponse<CircuitBreakerStatusDto>
            {
                Success = true,
                Data = MapToStatusDto(policy)
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ApiResponse<CircuitBreakerStatusDto> { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/dashboard/circuit-breakers/{name}/reset — Manually resets a circuit breaker to the Closed state.
    /// </summary>
    public Task<ApiResponse<CircuitBreakerStatusDto>> ResetBreakerAsync(string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                return Task.FromResult(new ApiResponse<CircuitBreakerStatusDto> { Success = false, Message = "Name is required" });

            var policy = _pipelineService.GetPolicyByName(name) as CircuitBreakerPolicy;
            if (policy is null)
                return Task.FromResult(new ApiResponse<CircuitBreakerStatusDto> { Success = false, Message = $"Circuit breaker '{name}' not found" });

            _circuitBreakerService.ResetCircuit(policy);

            return Task.FromResult(new ApiResponse<CircuitBreakerStatusDto>
            {
                Success = true,
                Data = MapToStatusDto(policy)
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ApiResponse<CircuitBreakerStatusDto> { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/dashboard/circuit-breakers/open — Returns only the circuit breakers currently in the Open state.
    /// </summary>
    public Task<ApiResponse<List<CircuitBreakerStatusDto>>> GetOpenBreakersAsync()
    {
        try
        {
            var open = GetAllCircuitBreakers()
                .Where(b => b.State == "Open")
                .ToList();

            return Task.FromResult(new ApiResponse<List<CircuitBreakerStatusDto>>
            {
                Success = true,
                Data = open.Select(b => new CircuitBreakerStatusDto
                {
                    PolicyId = b.PolicyId,
                    Name = b.Name,
                    State = b.State,
                    ConsecutiveFailures = b.ConsecutiveFailures,
                    FailureThreshold = b.FailureThreshold,
                    TripCount = b.TripCount,
                    SecondsUntilHalfOpen = b.SecondsUntilHalfOpen,
                    SuccessRate = b.SuccessRate,
                    TotalExecutions = b.TotalExecutions
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ApiResponse<List<CircuitBreakerStatusDto>> { Success = false, Message = ex.Message });
        }
    }

    // ─── helpers ──────────────────────────────────────────────────────────────

    private List<CircuitBreakerStatusDto> GetAllCircuitBreakers()
    {
        return _pipelineService
            .GetAllPolicies()
            .OfType<CircuitBreakerPolicy>()
            .Select(MapToStatusDto)
            .ToList();
    }

    private static CircuitBreakerStatusDto MapToStatusDto(CircuitBreakerPolicy policy)
    {
        policy.AttemptReset(); // refresh state before reporting

        return new CircuitBreakerStatusDto
        {
            PolicyId = policy.Id,
            Name = policy.Name,
            State = policy.CurrentState.ToString(),
            ConsecutiveFailures = policy.ConsecutiveFailures,
            FailureThreshold = policy.FailureThreshold,
            TripCount = policy.CircuitBreakerTrips,
            SecondsUntilHalfOpen = policy.TimeUntilHalfOpen?.TotalSeconds,
            SuccessRate = policy.GetSuccessRate(),
            TotalExecutions = policy.TotalExecutions,
            IsEnabled = policy.IsEnabled
        };
    }

    private static string ComputeOverallHealth(List<CircuitBreakerStatusDto> breakers)
    {
        if (breakers.Count == 0) return "Healthy";
        var openRatio = (double)breakers.Count(b => b.State == "Open") / breakers.Count;
        return openRatio switch
        {
            0 => "Healthy",
            <= 0.25 => "Degraded",
            _ => "Critical"
        };
    }
}

/// <summary>
/// Full dashboard snapshot for all circuit breakers.
/// </summary>
public sealed class CircuitBreakerDashboardDto
{
    /// <summary>Timestamp the dashboard was generated.</summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>Total number of registered circuit breakers.</summary>
    public int TotalBreakers { get; set; }

    /// <summary>Number of breakers in the Closed (healthy) state.</summary>
    public int ClosedCount { get; set; }

    /// <summary>Number of breakers in the Open (rejecting) state.</summary>
    public int OpenCount { get; set; }

    /// <summary>Number of breakers in the Half-Open (probing) state.</summary>
    public int HalfOpenCount { get; set; }

    /// <summary>Cumulative circuit trip count across all breakers.</summary>
    public long TotalTrips { get; set; }

    /// <summary>Derived overall health: Healthy, Degraded, or Critical.</summary>
    public string OverallHealth { get; set; } = "Healthy";

    /// <summary>Per-breaker status entries.</summary>
    public List<CircuitBreakerStatusDto> Breakers { get; set; } = new();
}

/// <summary>
/// Status snapshot for a single circuit breaker policy.
/// </summary>
public sealed class CircuitBreakerStatusDto
{
    /// <summary>Policy unique identifier.</summary>
    public string PolicyId { get; set; } = string.Empty;

    /// <summary>Policy name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Current state: Closed, Open, or HalfOpen.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Current run of consecutive failures.</summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>Failure threshold that triggers the Open state.</summary>
    public int FailureThreshold { get; set; }

    /// <summary>Total number of times this breaker has tripped.</summary>
    public long TripCount { get; set; }

    /// <summary>Seconds remaining until the breaker transitions to Half-Open (null when not Open).</summary>
    public double? SecondsUntilHalfOpen { get; set; }

    /// <summary>Lifetime success rate percentage.</summary>
    public double SuccessRate { get; set; }

    /// <summary>Total executions processed by this breaker.</summary>
    public long TotalExecutions { get; set; }

    /// <summary>Whether the policy is enabled.</summary>
    public bool IsEnabled { get; set; }
}
