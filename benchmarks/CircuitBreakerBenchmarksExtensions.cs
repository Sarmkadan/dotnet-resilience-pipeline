using System;
using BenchmarkDotNet.Attributes;

namespace DotNetResiliencePipeline.Benchmarks
{
    public static class CircuitBreakerBenchmarksExtensions
    {
        public static void WaitForState(this CircuitBreakerBenchmarks benchmarks, CircuitBreakerPolicy.CircuitState targetState, TimeSpan timeout)
        {
            var startTime = DateTime.UtcNow;
            while (benchmarks.CircuitBreaker_Get_CurrentState() != targetState)
            {
                if (DateTime.UtcNow - startTime > timeout)
                    throw new TimeoutException($"State did not transition to {targetState} within {timeout.TotalSeconds} seconds");
                System.Threading.Thread.Sleep(100);
            }
        }

        public static void AssertStateTransition(this CircuitBreakerBenchmarks benchmarks, CircuitBreakerPolicy.CircuitState fromState, CircuitBreakerPolicy.CircuitState toState, TimeSpan timeout)
        {
            var initialState = benchmarks.CircuitBreaker_Get_CurrentState();
            if (initialState != fromState)
                throw new InvalidOperationException($"Expected initial state {fromState}, got {initialState}");

            benchmarks.WaitForState(toState, timeout);
        }

        public static void TriggerFailures(this CircuitBreakerBenchmarks benchmarks, int failureCount)
        {
            for (int i = 0; i < failureCount; i++)
            {
                benchmarks.CircuitBreaker_Failure_Recording();
            }
        }

        public static void VerifyTripCount(this CircuitBreakerBenchmarks benchmarks, long expectedTrips)
        {
            var actualTrips = benchmarks.CircuitBreaker_Get_CircuitBreakerTrips();
            if (actualTrips != expectedTrips)
                throw new InvalidOperationException($"Expected {expectedTrips} trips, got {actualTrips}");
        }
    }
}
