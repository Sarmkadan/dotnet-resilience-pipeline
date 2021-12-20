#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Api.Controllers;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

/// <summary>
/// Provides unit tests for the <see cref="CircuitBreakerDashboardController"/> functionality.
/// Tests cover dashboard retrieval, breaker status monitoring, and circuit state management.
/// </summary>
public sealed class CircuitBreakerDashboardTests
{
	/// <summary>
	/// Creates a test fixture with a fresh <see cref="CircuitBreakerDashboardController"/> instance and its dependencies.
	/// </summary>
	/// <returns>A tuple containing the controller and pipeline service for testing.</returns>
	private static (CircuitBreakerDashboardController controller, ResiliencyPipelineService pipeline) BuildSut()
	{
		var pipeline = new ResiliencyPipelineService();
		var cbService = new CircuitBreakerService();
		var controller = new CircuitBreakerDashboardController(pipeline, cbService);
		return (controller, pipeline);
	}

	/// <summary>
	/// Tests that when no circuit breaker policies are registered, the dashboard returns an empty collection with a healthy overall status.
	/// </summary>
	[Fact]
	public async Task GetDashboard_NoPolicies_ReturnsEmptyHealthyDashboard()
	{
		var (controller, _) = BuildSut();

		var response = await controller.GetDashboardAsync();

		response.Success.Should().BeTrue();
		response.Data!.TotalBreakers.Should().Be(0);
		response.Data.OverallHealth.Should().Be("Healthy");
	}

	/// <summary>
	/// Tests that when a single circuit breaker policy is registered in a closed state, the dashboard correctly reports one breaker and one closed circuit.
	/// </summary>
	[Fact]
	public async Task GetDashboard_WithClosedBreaker_ReturnsClosedCount()
	{
		var (controller, pipeline) = BuildSut();
		pipeline.RegisterPolicy(new CircuitBreakerPolicy("svc-cb") { FailureThreshold = 5 });

		var response = await controller.GetDashboardAsync();

		response.Success.Should().BeTrue();
		response.Data!.TotalBreakers.Should().Be(1);
		response.Data.ClosedCount.Should().Be(1);
		response.Data.OpenCount.Should().Be(0);
	}

	/// <summary>
	/// Tests that when a circuit breaker policy is tripped by recording a failure, the dashboard reports one open breaker and degraded or critical overall health status.
	/// </summary>
	[Fact]
	public async Task GetDashboard_WithOpenBreaker_ReturnsOpenCountAndDegradedHealth()
	{
		var (controller, pipeline) = BuildSut();
		var policy = new CircuitBreakerPolicy("svc-cb") { FailureThreshold = 1 };
		pipeline.RegisterPolicy(policy);
		policy.RecordFailure(); // trips the circuit

		var response = await controller.GetDashboardAsync();

		response.Success.Should().BeTrue();
		response.Data!.OpenCount.Should().Be(1);
		response.Data.OverallHealth.Should().BeOneOf("Degraded", "Critical");
	}

	/// <summary>
	/// Tests that querying the status of a non-existent circuit breaker returns a not found response with an appropriate error message.
	/// </summary>
	[Fact]
	public async Task GetBreakerStatus_UnknownName_ReturnsNotFound()
	{
		var (controller, _) = BuildSut();

		var response = await controller.GetBreakerStatusAsync("does-not-exist");

		response.Success.Should().BeFalse();
		response.Message.Should().Contain("not found");
	}

	/// <summary>
	/// Tests that querying the status of an existing circuit breaker returns the correct breaker name and closed state.
	/// </summary>
	[Fact]
	public async Task GetBreakerStatus_ExistingBreaker_ReturnsCorrectState()
	{
		var (controller, pipeline) = BuildSut();
		pipeline.RegisterPolicy(new CircuitBreakerPolicy("order-cb") { FailureThreshold = 3 });

		var response = await controller.GetBreakerStatusAsync("order-cb");

		response.Success.Should().BeTrue();
		response.Data!.Name.Should().Be("order-cb");
		response.Data.State.Should().Be("Closed");
	}

	/// <summary>
	/// Tests that resetting an open circuit breaker transitions its state back to closed.
	/// </summary>
	[Fact]
	public async Task ResetBreaker_OpenCircuit_TransitionsToClosedState()
	{
		var (controller, pipeline) = BuildSut();
		var policy = new CircuitBreakerPolicy("reset-cb") { FailureThreshold = 1 };
		pipeline.RegisterPolicy(policy);
		policy.RecordFailure(); // open it

		policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.Open);

		var response = await controller.ResetBreakerAsync("reset-cb");

		response.Success.Should().BeTrue();
		response.Data!.State.Should().Be("Closed");
	}

	/// <summary>
	/// Tests that querying for open breakers returns only those breakers currently in the open state, filtering out closed breakers.
	/// </summary>
	[Fact]
	public async Task GetOpenBreakers_MixedStates_ReturnsOnlyOpenBreakers()
	{
		var (controller, pipeline) = BuildSut();

		var closed = new CircuitBreakerPolicy("closed-cb") { FailureThreshold = 10 };
		var open = new CircuitBreakerPolicy("open-cb") { FailureThreshold = 1 };
		pipeline.RegisterPolicy(closed);
		pipeline.RegisterPolicy(open);
		open.RecordFailure();

		var response = await controller.GetOpenBreakersAsync();

		response.Success.Should().BeTrue();
		response.Data!.Should().HaveCount(1);
		response.Data[0].Name.Should().Be("open-cb");
	}

	/// <summary>
	/// Tests that the trip count accumulates correctly across multiple trip-reset cycles for a circuit breaker policy.
	/// </summary>
	[Fact]
	public async Task GetDashboard_TripCountAccumulates_AcrossMultipleTrips()
	{
		var (controller, pipeline) = BuildSut();
		var policy = new CircuitBreakerPolicy("trip-cb")
		{
			FailureThreshold = 1,
			OpenDuration = TimeSpan.Zero
		};
		pipeline.RegisterPolicy(policy);

		// trip → reset → trip
		policy.RecordFailure();
		policy.ManualReset();
		policy.RecordFailure();

		var response = await controller.GetDashboardAsync();

		response.Data!.TotalTrips.Should().BeGreaterThanOrEqualTo(2);
	}
}
