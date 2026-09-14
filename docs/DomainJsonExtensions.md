# Domain JSON Extensions

This document provides method signatures, sample JSON, and usage examples for the JSON serialization and deserialization extension methods in the Domain and Utilities namespaces.

## PolicyResultJsonExtensions

Provides JSON serialization and deserialization extensions for `PolicyResult`.

### Methods

#### `ToJson(this PolicyResult value, bool indented = false)`

Serializes a `PolicyResult` instance to a JSON string.

**Parameters:**
- `value`: The policy result to serialize.
- `indented`: Whether to format the JSON with indentation for readability.

**Returns:** A JSON string representation of the policy result.

**Exceptions:**
- `ArgumentNullException`: Thrown when `value` is `null`.

**Sample JSON (non-indented):**
```json
{
  "isSuccess": true,
  "data": {"value": 42},
  "policyName": "RetryPolicy",
  "executionTimeMs": 150,
  "attemptCount": 3,
  "exception": null,
  "executedAt": "2026-09-14T10:30:00Z",
  "executionId": "a1b2c3d4-e5f6-7890-g1h2-i3j4k5l6m7n8",
  "metadata": {}
}
```

**Sample JSON (indented):**
```json
{
  "isSuccess": true,
  "data": {
    "value": 42
  },
  "policyName": "RetryPolicy",
  "executionTimeMs": 150,
  "attemptCount": 3,
  "exception": null,
  "executedAt": "2026-09-14T10:30:00Z",
  "executionId": "a1b2c3d4-e5f6-7890-g1h2-i3j4k5l6m7n8",
  "metadata": {}
}
```

**Usage Example:**
```csharp
var result = PolicyResult<int>.Success(42, "RetryPolicy", 150, 3);
string json = result.ToJson(); // Compact JSON
string prettyJson = result.ToJson(indented: true); // Formatted JSON
```

#### `FromJson(string json)`

Deserializes a JSON string to a `PolicyResult` instance.

**Parameters:**
- `json`: The JSON string to deserialize.

**Returns:** The deserialized policy result, or `null` if the JSON is empty or whitespace.

**Exceptions:**
- `ArgumentNullException`: Thrown when `json` is `null`.
- `ArgumentException`: Thrown when `json` is empty or whitespace.
- `JsonException`: Thrown when the JSON is invalid or cannot be deserialized.

**Usage Example:**
```csharp
string json = @"{""isSuccess"":true,""data"":{""value"":42},""policyName"":""RetryPolicy"",""executionTimeMs"":150,""attemptCount"":3}";
PolicyResult<int>? result = PolicyResultJsonExtensions.FromJson(json);
```

#### `TryFromJson(string json, out PolicyResult? value)`

Attempts to deserialize a JSON string to a `PolicyResult` instance.

**Parameters:**
- `json`: The JSON string to deserialize.
- `value`: Receives the deserialized policy result if successful.

**Returns:** `true` if deserialization succeeded; otherwise, `false`.

**Exceptions:**
- `ArgumentNullException`: Thrown when `json` is `null`.

**Usage Example:**
```csharp
string json = @"{""isSuccess"":true,""data"":{""value"":42},""policyName"":""RetryPolicy"",""executionTimeMs"":150,""attemptCount"":3}";
if (PolicyResultJsonExtensions.TryFromJson(json, out PolicyResult<int>? result))
{
    // Use result
}
```

---

## PipelineMetricsSnapshotJsonExtensions

Provides JSON serialization extensions for `PipelineMetricsSnapshot`.

### Methods

#### `ToJson(this PipelineMetricsSnapshot snapshot)`

Serializes a `PipelineMetricsSnapshot` instance to a JSON string.

**Parameters:**
- `snapshot`: The pipeline metrics snapshot to serialize.

**Returns:** A JSON string representation of the pipeline metrics snapshot.

**Exceptions:**
- `ArgumentNullException`: Thrown when `snapshot` is `null`.

**Sample JSON:**
```json
{
  "totalExecutions": 1250,
  "successfulExecutions": 1180,
  "failedExecutions": 70,
  "successRate": 94.4,
  "retryCount": 45,
  "circuitBreakerTrips": 3,
  "timeoutCount": 2,
  "policySnapshots": [
    {
      "policyName": "RetryPolicy",
      "totalExecutions": 500,
      "successfulExecutions": 475,
      "failedExecutions": 25,
      "successRate": 95.0,
      "averageExecutionTimeMs": 120.5,
      "retryCount": 30,
      "circuitBreakerTrips": 0,
      "timeoutCount": 0
    },
    {
      "policyName": "CircuitBreakerPolicy",
      "totalExecutions": 750,
      "successfulExecutions": 705,
      "failedExecutions": 45,
      "successRate": 94.0,
      "averageExecutionTimeMs": 85.2,
      "retryCount": 15,
      "circuitBreakerTrips": 3,
      "timeoutCount": 2
    }
  ]
}
```

**Usage Example:**
```csharp
var snapshot = new PipelineMetricsSnapshot
{
    TotalExecutions = 1250,
    SuccessfulExecutions = 1180,
    FailedExecutions = 70,
    SuccessRate = 94.4,
    RetryCount = 45,
    CircuitBreakerTrips = 3,
    TimeoutCount = 2,
    PolicySnapshots = new List<Policies.PolicySnapshot> { /* populated */ }
};
string json = PipelineMetricsSnapshotJsonExtensions.ToJson(snapshot);
```

---

## MetricsAggregatorJsonExtensions

Provides System.Text.Json serialization and deserialization extensions for `MetricsAggregator`.

### Methods

#### `ToJson(this MetricsAggregator value, bool indented = false)`

Serializes the `MetricsAggregator` instance to a JSON string.

**Parameters:**
- `value`: The metrics aggregator to serialize.
- `indented`: Whether to format the JSON with indentation.

**Returns:** A JSON string representation of the metrics aggregator.

**Exceptions:**
- `ArgumentNullException`: Thrown when `value` is `null`.

**Sample JSON (non-indented):**
```json
{
  "maxSnapshots": 1000,
  "_snapshots": [
    {
      "timestamp": "2026-09-14T10:00:00Z",
      "totalExecutions": 100,
      "successfulExecutions": 95,
      "failedExecutions": 5,
      "successRate": 95.0,
      "averageExecutionTimeMs": 120.5,
      "activePolicies": 3
    },
    {
      "timestamp": "2026-09-14T10:05:00Z",
      "totalExecutions": 110,
      "successfulExecutions": 105,
      "failedExecutions": 5,
      "successRate": 95.5,
      "averageExecutionTimeMs": 115.2,
      "activePolicies": 3
    }
  ],
  "_lockObj": {}
}
```

**Sample JSON (indented):**
```json
{
  "maxSnapshots": 1000,
  "_snapshots": [
    {
      "timestamp": "2026-09-14T10:00:00Z",
      "totalExecutions": 100,
      "successfulExecutions": 95,
      "failedExecutions": 5,
      "successRate": 95.0,
      "averageExecutionTimeMs": 120.5,
      "activePolicies": 3
    },
    {
      "timestamp": "2026-09-14T10:05:00Z",
      "totalExecutions": 110,
      "successfulExecutions": 105,
      "failedExecutions": 5,
      "successRate": 95.5,
      "averageExecutionTimeMs": 115.2,
      "activePolicies": 3
    }
  ],
  "_lockObj": {}
}
```

**Usage Example:**
```csharp
var aggregator = new MetricsAggregator();
// Add some snapshots...
string json = aggregator.ToJson(); // Compact JSON
string prettyJson = aggregator.ToJson(indented: true); // Formatted JSON
```

#### `FromJson(string json)`

Deserializes a JSON string to a `MetricsAggregator` instance.

**Parameters:**
- `json`: The JSON string to deserialize.

**Returns:** A deserialized `MetricsAggregator` instance, or `null` if deserialization fails.

**Exceptions:**
- `ArgumentNullException`: Thrown when `json` is `null`.
- `ArgumentException`: Thrown when `json` is empty or whitespace.

**Usage Example:**
```csharp
string json = @"{""maxSnapshots"":1000,""_snapshots"":[{""timestamp"":""2026-09-14T10:00:00Z"",""totalExecutions"":100,""successfulExecutions"":95,""failedExecutions"":5,""successRate"":95.0,""averageExecutionTimeMs"":120.5,""activePolicies"":3}]}";
MetricsAggregator? aggregator = MetricsAggregatorJsonExtensions.FromJson(json);
```

#### `TryFromJson(string json, out MetricsAggregator? value)`

Attempts to deserialize a JSON string to a `MetricsAggregator` instance.

**Parameters:**
- `json`: The JSON string to deserialize.
- `value`: Receives the deserialized instance if successful.

**Returns:** `True` if deserialization succeeds; otherwise, `false`.

**Exceptions:**
- `ArgumentNullException`: Thrown when `json` is `null`.
- `ArgumentException`: Thrown when `json` is empty or whitespace.

**Usage Example:**
```csharp
string json = @"{""maxSnapshots"":1000,""_snapshots"":[{""timestamp"":""2026-09-14T10:00:00Z"",""totalExecutions"":100,""successfulExecutions"":95,""failedExecutions"":5,""successRate"":95.0,""averageExecutionTimeMs"":120.5,""activePolicies"":3}]}";
if (MetricsAggregatorJsonExtensions.TryFromJson(json, out MetricsAggregator? aggregator))
{
    // Use aggregator
}
```

---

## CircuitBreakerDashboardControllerJsonExtensions

Provides System.Text.Json serialization extensions for `CircuitBreakerDashboardController` and its related DTO types.

### Methods

#### `ToJson(this CircuitBreakerDashboardController value, bool indented = false)`

Serializes the `CircuitBreakerDashboardController` instance to a JSON string.

**Parameters:**
- `value`: The controller instance to serialize.
- `indented`: Whether to format the JSON with indentation for readability.

**Returns:** A JSON string representation of the controller.

**Exceptions:**
- `ArgumentNullException`: Thrown when `value` is `null`.

**Sample JSON (non-indented):**
```json
{
  "_pipelineService": {},
  "_circuitBreakerService": {}
}
```

**Sample JSON (indented):**
```json
{
  "_pipelineService": {},
  "_circuitBreakerService": {}
}
```

**Usage Example:**
```csharp
var controller = new CircuitBreakerDashboardController(pipelineService, circuitBreakerService);
string json = controller.ToJson(); // Compact JSON
string prettyJson = controller.ToJson(indented: true); // Formatted JSON
```

#### `FromJson(string json)`

Deserializes a JSON string into a `CircuitBreakerDashboardController` instance.

**Parameters:**
- `json`: The JSON string to deserialize.

**Returns:** The deserialized controller instance, or `null` if deserialization fails.

**Exceptions:**
- `ArgumentNullException`: Thrown when `json` is `null`.
- `JsonException`: Thrown when the JSON is invalid or cannot be deserialized.

**Usage Example:**
```csharp
string json = @"{""_pipelineService"":{},""_circuitBreakerService"":{}}";
CircuitBreakerDashboardController? controller = CircuitBreakerDashboardControllerJsonExtensions.FromJson(json);
```

#### `TryFromJson(string json, out CircuitBreakerDashboardController? value)`

Attempts to deserialize a JSON string into a `CircuitBreakerDashboardController` instance.

**Parameters:**
- `json`: The JSON string to deserialize. Must not be null or whitespace.
- `value`: Receives the deserialized controller instance if successful; otherwise, `null`.

**Returns:** `True` if deserialization succeeds; otherwise, `false`.

**Exceptions:**
- `ArgumentNullException`: Thrown when `json` is `null`.

**Usage Example:**
```csharp
string json = @"{""_pipelineService"":{},""_circuitBreakerService"":{}}";
if (CircuitBreakerDashboardControllerJsonExtensions.TryFromJson(json, out CircuitBreakerDashboardController? controller))
{
    // Use controller
}
```