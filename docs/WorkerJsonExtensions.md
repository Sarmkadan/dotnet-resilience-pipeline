# Worker Json Extensions

Provides JSON serialization and deserialization extensions for worker classes.

## MetricsCollectorWorkerJsonExtensions

Provides JSON serialization and deserialization extensions for <see cref="MetricsCollectorWorker"/>.

### ToJson(MetricsCollectorWorker value, bool indented = false)

Serializes the specified <see cref="MetricsCollectorWorker"/> instance to a JSON string.

#### Signature
```csharp
public static string ToJson(this MetricsCollectorWorker value, bool indented = false)
```

#### Parameters
- `value`: The <see cref="MetricsCollectorWorker"/> instance to serialize.
- `indented`: Indicates whether the JSON output should be indented.

#### Returns
A JSON string representation of the <paramref name="value"/>.

#### Exceptions
- <see cref="ArgumentNullException">: <paramref name="value"/> is <c>null</c>.

#### Sample JSON
```json
{
  "workerId": "worker-123",
  "lastHeartbeat": "2026-09-14T10:30:00Z",
  "isRunning": true,
  "metricsCollected": 15420,
  "configuration": {
    "collectionIntervalSeconds": 30,
    "batchSize": 100
  }
}
```

#### Example
```csharp
var worker = new MetricsCollectorWorker
{
    WorkerId = "worker-123",
    LastHeartbeat = DateTime.UtcNow,
    IsRunning = true,
    MetricsCollected = 15420,
    Configuration = new WorkerConfiguration { CollectionIntervalSeconds = 30, BatchSize = 100 }
};

string json = worker.ToJson(indented: true);
// json contains formatted JSON representation of the worker
```

### FromJson(string json)

Deserializes the specified JSON string into a <see cref="MetricsCollectorWorker"/> instance.

#### Signature
```csharp
public static MetricsCollectorWorker FromJson(string json)
```

#### Parameters
- `json`: The JSON string to deserialize.

#### Returns
The deserialized <see cref="MetricsCollectorWorker"/> instance.

#### Exceptions
- <see cref="ArgumentNullException">: <paramref name="json"/> is <c>null</c>.
- <see cref="ArgumentException">: <paramref name="json"/> is empty.
- <see cref="JsonException">: The JSON is invalid or cannot be deserialized to a <see cref="MetricsCollectorWorker"/>.

#### Example
```csharp
string json = @"{
  ""workerId"": ""worker-456"",
  ""lastHeartbeat"": ""2026-09-14T11:00:00Z"",
  ""isRunning"": false,
  ""metricsCollected"": 8920,
  ""configuration"": {
    ""collectionIntervalSeconds"": 60,
    ""batchSize"": 50
  }
}";

MetricsCollectorWorker worker = MetricsCollectorWorkerJsonExtensions.FromJson(json);
// worker.WorkerId == "worker-456"
// worker.IsRunning == false
```

### TryFromJson(string json, out MetricsCollectorWorker? value)

Attempts to deserialize the specified JSON string into a <see cref="MetricsCollectorWorker"/> instance.

#### Signature
```csharp
public static bool TryFromJson(string json, out MetricsCollectorWorker? value)
```

#### Parameters
- `json`: The JSON string to deserialize.
- `value`: When this method returns, contains the deserialized <see cref="MetricsCollectorWorker"/> instance if successful; otherwise, <c>null</c>.

#### Returns
<c>true</c> if deserialization succeeded; otherwise, <c>false</c>.

#### Exceptions
- <see cref="ArgumentNullException">: <paramref name="json"/> is <c>null</c>.
- <see cref="ArgumentException">: <paramref name="json"/> is empty.

#### Example
```csharp
string json = @"{ ""workerId"": ""worker-789"" }"; // Missing required properties

if (MetricsCollectorWorkerJsonExtensions.TryFromJson(json, out var worker))
{
    // Deserialization succeeded
    Console.WriteLine($"Worker ID: {worker.WorkerId}");
}
else
{
    // Deserialization failed
    Console.WriteLine("Invalid JSON for MetricsCollectorWorker");
}
```

## HealthReportJsonExtensions

Provides JSON serialization extensions for <see cref="HealthReport"/>.

### ToJson(HealthReport report)

Serializes the specified <see cref="HealthReport"/> to a JSON string.

#### Signature
```csharp
public static string ToJson(this HealthReport report)
```

#### Parameters
- `report`: The health report to serialize.

#### Returns
A JSON string representation of the health report.

#### Exceptions
- <see cref="ArgumentNullException">: Thrown when <paramref name="report"/> is null.

#### Sample JSON
```json
{
  "status": "Healthy",
  "timestamp": "2026-09-14T10:30:00Z",
  "checks": [
    {
      "name": "DatabaseConnection",
      "status": "Healthy",
      "duration": "00:00:00.1234567",
      "description": "Database connection is responsive"
    },
    {
      "name": "CacheService",
      "status": "Degraded",
      "duration": "00:00:00.0456789",
      "description": "Cache service responding slowly"
    }
  ]
}
```

#### Example
```csharp
var report = new HealthReport
{
    Status = HealthStatus.Healthy,
    Timestamp = DateTime.UtcNow,
    Checks = new[]
    {
        new HealthCheck { Name = "DatabaseConnection", Status = HealthStatus.Healthy, Duration = TimeSpan.FromMilliseconds(123.4567), Description = "Database connection is responsive" },
        new HealthCheck { Name = "CacheService", Status = HealthStatus.Degraded, Duration = TimeSpan.FromMilliseconds(45.6789), Description = "Cache service responding slowly" }
    }
};

string json = report.ToJson();
// json contains JSON representation of the health report
```