# CircuitBreakerDashboardTests
The `CircuitBreakerDashboardTests` class is designed to test the functionality of a circuit breaker dashboard, which is responsible for monitoring and managing circuit breakers in a system. This class contains a set of test methods that verify the correct behavior of the dashboard under various scenarios, including retrieving the dashboard, getting the status of a circuit breaker, resetting a circuit breaker, and retrieving open circuit breakers.

## API
The `CircuitBreakerDashboardTests` class contains the following public members:
* `GetDashboard_NoPolicies_ReturnsEmptyHealthyDashboard`: Tests that the dashboard returns an empty and healthy state when there are no policies.
* `GetDashboard_WithClosedBreaker_ReturnsClosedCount`: Tests that the dashboard returns the correct count of closed circuit breakers.
* `GetDashboard_WithOpenBreaker_ReturnsOpenCountAndDegradedHealth`: Tests that the dashboard returns the correct count of open circuit breakers and a degraded health state.
* `GetBreakerStatus_UnknownName_ReturnsNotFound`: Tests that the dashboard returns a not found status when an unknown circuit breaker name is provided.
* `GetBreakerStatus_ExistingBreaker_ReturnsCorrectState`: Tests that the dashboard returns the correct state of an existing circuit breaker.
* `ResetBreaker_OpenCircuit_TransitionsToClosedState`: Tests that resetting an open circuit breaker transitions it to a closed state.
* `GetOpenBreakers_MixedStates_ReturnsOnlyOpenBreakers`: Tests that the dashboard returns only open circuit breakers when there are mixed states.
* `GetDashboard_TripCountAccumulates_AcrossMultipleTrips`: Tests that the trip count accumulates across multiple trips.

## Usage
Here are two examples of using the `CircuitBreakerDashboardTests` class:
```csharp
// Example 1: Testing the dashboard with a closed circuit breaker
var dashboard = new CircuitBreakerDashboard();
var breaker = new CircuitBreaker("breaker1");
breaker.Close();
var result = await dashboard.GetDashboard();
Assert.AreEqual(1, result.ClosedCount);

// Example 2: Testing the reset of an open circuit breaker
var dashboard = new CircuitBreakerDashboard();
var breaker = new CircuitBreaker("breaker2");
breaker.Open();
await dashboard.ResetBreaker(breaker.Name);
var result = await dashboard.GetBreakerStatus(breaker.Name);
Assert.AreEqual(CircuitBreakerState.Closed, result.State);
```

## Notes
The `CircuitBreakerDashboardTests` class is designed to be thread-safe, as it uses asynchronous methods to test the dashboard. However, it is still important to ensure that the tests are run in a controlled environment to avoid any potential conflicts. Additionally, the class assumes that the circuit breaker dashboard is properly configured and that the circuit breakers are in a valid state. If the circuit breakers are not properly configured or are in an invalid state, the tests may not behave as expected. It is also worth noting that the `GetDashboard_TripCountAccumulates_AcrossMultipleTrips` test method relies on the trip count accumulating across multiple trips, which may not be the case if the circuit breaker is reset or if the trip count is manually modified.
