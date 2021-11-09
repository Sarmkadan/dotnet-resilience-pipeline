# MetricsAggregatorTests

The `MetricsAggregatorTests` class serves as the comprehensive test suite for the `MetricsAggregator` component within the `dotnet-resilience-pipeline` project. It validates the correctness of metric collection, aggregation logic, historical snapshot management, and trend analysis algorithms. By exercising scenarios ranging from empty state initialization to complex anomaly detection, this class ensures that the aggregator accurately tracks execution statistics, maintains data integrity under capacity constraints, and provides reliable insights into system resilience trends.

## API

The following public members define the test coverage for the aggregator's functionality:

### `RecordSnapshot_AddsSnapshotToHistory`
Verifies that invoking the snapshot recording mechanism successfully appends a new metrics snapshot to the internal history collection. This test confirms the basic ingestion path without asserting aggregation logic.
*   **Parameters**: None (uses test fixtures).
*   **Return Value**: `void`.
*   **Throws**: Throws an assertion exception if the history count does not increment or if the snapshot data is not preserved.

### `GetAggregatedMetrics_EmptyHistory_ReturnsDefaultAggregatedMetrics`
Validates the behavior of the aggregation method when no snapshots have been recorded. It ensures the system returns a defined default state (e.g., zero counts, null averages) rather than throwing exceptions or returning uninitialized data.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Throws**: Throws if the returned metrics differ from the expected default values.

### `GetAggregatedMetrics_MultipleSnapshots_AveragesSuccessRate`
Confirms that when multiple snapshots exist, the aggregated success rate is calculated as the mathematical mean of the individual snapshot success rates.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Throws**: Throws if the calculated average deviates from the expected precision.

### `GetAggregatedMetrics_SumsTotalExecutions`
Ensures that the `TotalExecutions` field in the aggregated result represents the cumulative sum of executions across all stored snapshots.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Throws**: Throws if the sum is incorrect.

### `GetAggregatedMetrics_TracksPeakExecutions`
Verifies that the aggregator correctly identifies and records the maximum number of executions observed in any single snapshot within the history.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Throws**: Throws if the peak value does not match the highest individual snapshot value.

### `GetAggregatedMetrics_TracksMinAndMaxSuccessRate`
Validates that the aggregated metrics include the boundary values for success rates, specifically tracking both the minimum and maximum success rates observed across the history.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Throws**: Throws if min/max values are not accurately reflected.

### `MaxSnapshots_WhenExceeded_EvictsOldestSnapshot`
Tests the capacity enforcement logic. When the number of recorded snapshots exceeds the configured maximum limit, this test verifies that the oldest snapshot is removed (FIFO eviction) to maintain the size constraint.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Throws**: Throws if the collection size exceeds the limit or if the wrong snapshot (non-oldest) is evicted.

### `AnalyzeTrend_InsufficientData_ReturnsEmptyTrend`
Ensures that the trend analysis method returns an empty or neutral trend result when the available history contains fewer snapshots than the minimum required for statistical significance.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Throws**: Throws if a trend direction is incorrectly inferred from insufficient data.

### `AnalyzeTrend_ImprovingSuccessRate_ReturnsIncreasingDirection`
Validates that a sequence of snapshots showing a consistent rise in success rates results in a trend analysis indicating an "Increasing" direction.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Throws**: Throws if the direction is not marked as increasing.

### `AnalyzeTrend_DecliningSuccessRate_ReturnsDecreasingDirection`
Validates that a sequence of snapshots showing a consistent drop in success rates results in a trend analysis indicating a "Decreasing" direction.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Throws**: Throws if the direction is not marked as decreasing.

### `AnalyzeTrend_LargeChangePercentage_MarksAsAnomaly`
Confirms that the trend analyzer flags a result as an anomaly when the percentage change between snapshots exceeds a predefined threshold, indicating volatile or erratic behavior.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Throws**: Throws if the anomaly flag is not set despite large variance.

### `Clear_RemovesAllSnapshots`
Verifies that the clear operation empties the internal history collection completely, resetting the aggregator to its initial state.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Throws**: Throws if any snapshots remain after the operation.

## Usage

The following examples demonstrate how the test class validates specific behaviors of the `MetricsAggregator`.

### Example 1: Validating Aggregation Logic
This test scenario records multiple snapshots and verifies that the aggregated metrics correctly compute the average success rate and total executions.

```csharp
[Fact]
public void GetAggregatedMetrics_MultipleSnapshots_AveragesSuccessRate()
{
    // Arrange
    var aggregator = new MetricsAggregator(maxSnapshots: 10);
    aggregator.RecordSnapshot(new MetricsSnapshot { SuccessRate = 0.80, Executions = 100 });
    aggregator.RecordSnapshot(new MetricsSnapshot { SuccessRate = 0.90, Executions = 100 });

    // Act
    var aggregated = aggregator.GetAggregatedMetrics();

    // Assert
    Assert.Equal(0.85, aggregated.AverageSuccessRate, 0.01);
    Assert.Equal(200, aggregated.TotalExecutions);
}
```

### Example 2: Verifying Capacity Eviction
This scenario ensures that when the snapshot limit is reached, adding a new entry removes the oldest entry, preserving only the most recent data.

```csharp
[Fact]
public void MaxSnapshots_WhenExceeded_EvictsOldestSnapshot()
{
    // Arrange
    var aggregator = new MetricsAggregator(maxSnapshots: 2);
    aggregator.RecordSnapshot(new MetricsSnapshot { Timestamp = DateTime.UtcNow.AddMinutes(-2), Value = 1 });
    aggregator.RecordSnapshot(new MetricsSnapshot { Timestamp = DateTime.UtcNow.AddMinutes(-1), Value = 2 });

    // Act
    aggregator.RecordSnapshot(new MetricsSnapshot { Timestamp = DateTime.UtcNow, Value = 3 });

    // Assert
    var history = aggregator.GetHistory();
    Assert.Equal(2, history.Count);
    Assert.DoesNotContain(history, s => s.Value == 1); // Oldest removed
    Assert.Contains(history, s => s.Value == 3); // Newest present
}
```

## Notes

*   **Thread Safety**: The test signatures imply sequential execution patterns typical of unit tests. While the tests validate logical correctness, they do not inherently prove thread safety of the underlying `MetricsAggregator`. If the production component is intended for concurrent access, additional stress tests involving parallel `RecordSnapshot` and `GetAggregatedMetrics` calls are recommended, as the current suite focuses on state transitions in a single-threaded context.
*   **Edge Cases**:
    *   **Empty State**: The `GetAggregatedMetrics_EmptyHistory_ReturnsDefaultAggregatedMetrics` test highlights the importance of handling zero-data scenarios gracefully. Consumers of the aggregator should expect valid default objects rather than nulls when no data is present.
    *   **Statistical Significance**: The `AnalyzeTrend_InsufficientData_ReturnsEmptyTrend` test establishes a minimum data threshold. Trend analysis should not be attempted or trusted unless the history contains enough samples to smooth out transient noise.
    *   **Precision**: Tests involving averages and success rates (e.g., `AveragesSuccessRate`) may be subject to floating-point precision issues. Implementations should ensure consistent rounding strategies to prevent flaky tests.
*   **Eviction Policy**: The `MaxSnapshots_WhenExceeded_EvictsOldestSnapshot` test confirms a First-In-First-Out (FIFO) policy. Systems relying on this aggregator for sliding window analysis must account for the fact that older data is permanently discarded once the buffer is full, which may impact long-term trend accuracy if the window size is too small.
