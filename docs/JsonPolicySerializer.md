# JsonPolicySerializer

A serializer and deserializer for resiliency policies, enabling conversion between policy objects and JSON representations for storage or transmission. It supports both single policies and collections, and provides file-based import/export for persistence.

## API

### `public JsonPolicySerializer`

Initializes a new instance of the `JsonPolicySerializer` with default settings.

### `public string Serialize(ResiliencyPolicy policy)`

Serializes a single `ResiliencyPolicy` object into a JSON string.

- **Parameters**
  - `policy`: The policy instance to serialize.
- **Return value**
  - A JSON string representing the policy.
- **Exceptions**
  - Throws `ArgumentNullException` if `policy` is `null`.

### `public string SerializeMultiple(IEnumerable<ResiliencyPolicy> policies)`

Serializes a collection of `ResiliencyPolicy` objects into a JSON string.

- **Parameters**
  - `policies`: The collection of policies to serialize.
- **Return value**
  - A JSON string representing the collection.
- **Exceptions**
  - Throws `ArgumentNullException` if `policies` is `null`.

### `public ResiliencyPolicy? Deserialize(string json)`

Deserializes a JSON string into a `ResiliencyPolicy` object.

- **Parameters**
  - `json`: The JSON string to deserialize.
- **Return value**
  - The deserialized `ResiliencyPolicy` instance, or `null` if the JSON is invalid or empty.
- **Exceptions**
  - Throws `JsonException` if the JSON is malformed and cannot be parsed.

### `public string SerializeMetrics(ResiliencyMetrics metrics)`

Serializes a `ResiliencyMetrics` object into a JSON string.

- **Parameters**
  - `metrics`: The metrics object to serialize.
- **Return value**
  - A JSON string representing the metrics.
- **Exceptions**
  - Throws `ArgumentNullException` if `metrics` is `null`.

### `public async Task ExportToFileAsync(string filePath, string json)`

Writes a JSON string to a file asynchronously.

- **Parameters**
  - `filePath`: The path to the output file.
  - `json`: The JSON content to write.
- **Exceptions**
  - Throws `ArgumentNullException` if `filePath` or `json` is `null`.
  - Throws `UnauthorizedAccessException` if the caller lacks required permissions.
  - Throws `DirectoryNotFoundException` if the parent directory does not exist.
  - Throws `IOException` on file system errors.

### `public async Task<List<ResiliencyPolicy>> ImportFromFileAsync(string filePath)`

Reads a JSON file containing serialized policies asynchronously and deserializes it into a list.

- **Parameters**
  - `filePath`: The path to the input file.
- **Return value**
  - A list of deserialized `ResiliencyPolicy` instances.
- **Exceptions**
  - Throws `ArgumentNullException` if `filePath` is `null`.
  - Throws `FileNotFoundException` if the file does not exist.
  - Throws `UnauthorizedAccessException` if the caller lacks required permissions.
  - Throws `JsonException` if the file content is invalid JSON.
  - Throws `IOException` on file system errors.

### `public string Id`

Gets the unique identifier of the serializer instance.

- **Return value**
  - A string representing the identifier.

### `public string Name`

Gets the display name of the serializer instance.

- **Return value**
  - A string representing the name.

### `public string Type`

Gets the type identifier of the serializer.

- **Return value**
  - A string representing the type.

### `public bool IsEnabled`

Gets or sets whether the serializer is enabled for use.

- **Return value**
  - `true` if enabled; otherwise, `false`.

### `public DateTime CreatedAt`

Gets the timestamp when the serializer instance was created.

- **Return value**
  - A `DateTime` representing the creation time.

### `public int? FailureThreshold`

Gets or sets the failure threshold for the policy.

- **Return value**
  - An optional integer representing the threshold.

### `public int? OpenDurationSeconds`

Gets or sets the open duration in seconds for the policy.

- **Return value**
  - An optional integer representing the duration.

### `public int? SuccessThreshold`

Gets or sets the success threshold for the policy.

- **Return value**
  - An optional integer representing the threshold.

### `public int? MaxRetries`

Gets or sets the maximum number of retry attempts.

- **Return value**
  - An optional integer representing the retry count.

### `public int? InitialDelayMs`

Gets or sets the initial delay in milliseconds before the first retry.

- **Return value**
  - An optional integer representing the delay.

### `public string? Strategy`

Gets or sets the retry strategy (e.g., "Exponential", "Linear").

- **Return value**
  - An optional string representing the strategy.

### `public double? BackoffMultiplier`

Gets or sets the backoff multiplier for exponential retry strategies.

- **Return value**
  - An optional double representing the multiplier.

### `public int? TimeoutSeconds`

Gets or sets the timeout in seconds for the policy operation.

- **Return value**
  - An optional integer representing the timeout.

## Usage

### Example 1: Serialize and Export a Single Policy
