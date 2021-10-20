# FailureInjectionService

A service for injecting controlled failures into resilient pipelines, enabling deterministic testing of fault-handling strategies. It allows rules to be defined that trigger exceptions, latency, timeouts, or other faults based on configurable conditions such as injection rate, exception type, or custom predicates.

## API

### `public long TotalInjections`

Gets the total number of fault injections that have occurred across all rules since the service was created.

- **Type**: `long`
- **Access**: Read-only
- **Thread Safety**: Safe for concurrent reads.

---

### `public FailureInjectionService()`

Initializes a new instance of the `FailureInjectionService` with no rules and all fault injection disabled.

- **Thread Safety**: Safe for initialization in multi-threaded contexts.

---

### `public void AddRule(InjectionRule rule)`

Adds a new fault injection rule to the service. If a rule with the same `RuleKey` already exists, it will be replaced.

- **Parameters**:
  - `rule` – The `InjectionRule` to add.
- **Exceptions**:
  - Throws `ArgumentNullException` if `rule` is `null`.
- **Thread Safety**: Safe for concurrent calls.

---

### `public bool RemoveRule(string ruleKey)`

Removes the fault injection rule identified by `ruleKey`.

- **Parameters**:
  - `ruleKey` – The unique key of the rule to remove.
- **Return Value**: `true` if the rule was found and removed; otherwise, `false`.
- **Thread Safety**: Safe for concurrent calls.

---

### `public IReadOnlyList<InjectionRule> GetRules()`

Returns a read-only snapshot of all currently registered fault injection rules.

- **Return Value**: An `IReadOnlyList<InjectionRule>` containing all active rules.
- **Thread Safety**: Safe for concurrent reads; the returned list is immutable.

---

### `public void DisableAll()`

Disables all fault injection rules in the service, preventing any further faults from being injected.

- **Thread Safety**: Safe for concurrent calls.

---

### `public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)`

Executes the provided asynchronous function while applying any matching fault injection rules. If a rule matches, the configured fault (exception, latency, timeout) is injected before invoking the action.

- **Type Parameters**:
  - `T` – The return type of the action.
- **Parameters**:
  - `action` – The asynchronous function to execute.
- **Return Value**: The result of `action` if no fault is injected; otherwise, the fault is thrown or the delay is applied.
- **Exceptions**:
  - Throws `InjectedFaultException` if a matching rule injects an exception.
  - Throws `TimeoutException` if a matching rule injects a timeout and the action exceeds `TimeoutDuration`.
  - Throws `OperationCanceledException` if cancellation is requested and a matching rule injects a timeout.
- **Thread Safety**: Safe for concurrent calls; each execution is isolated.

---

### `public async Task ExecuteAsync(Func<Task> action)`

Executes the provided asynchronous action while applying any matching fault injection rules. If a rule matches, the configured fault is injected before invoking the action.

- **Parameters**:
  - `action` – The asynchronous action to execute.
- **Exceptions**:
  - Throws `InjectedFaultException` if a matching rule injects an exception.
  - Throws `TimeoutException` if a matching rule injects a timeout and the action exceeds `TimeoutDuration`.
  - Throws `OperationCanceledException` if cancellation is requested and a matching rule injects a timeout.
- **Thread Safety**: Safe for concurrent calls; each execution is isolated.

---
### `public string Key`

Gets the unique identifier for this service instance.

- **Type**: `string`
- **Access**: Read-only
- **Thread Safety**: Safe for concurrent reads.

---
### `public InjectionType Type`

Gets or sets the default fault type injected when no rule-specific type is defined.

- **Type**: `InjectionType`
- **Access**: Read-write
- **Thread Safety**: Safe for concurrent reads; writes are not synchronized and should be performed during initialization or with external synchronization.

---
### `public bool IsEnabled`

Gets or sets whether fault injection is globally enabled for this service.

- **Type**: `bool`
- **Access**: Read-write
- **Thread Safety**: Safe for concurrent reads; writes are not synchronized and should be performed during initialization or with external synchronization.

---
### `public double InjectionRate`

Gets or sets the default probability (between 0.0 and 1.0) that a fault will be injected when no rule-specific rate is defined.

- **Type**: `double`
- **Access**: Read-write
- **Thread Safety**: Safe for concurrent reads; writes are not synchronized and should be performed during initialization or with external synchronization.

---
### `public string? ExceptionMessage`

Gets or sets the default exception message used when injecting exceptions and no rule-specific message is provided.

- **Type**: `string?`
- **Access**: Read-write
- **Thread Safety**: Safe for concurrent reads; writes are not synchronized and should be performed during initialization or with external synchronization.

---
### `public Func<Exception>? ExceptionFactory`

Gets or sets the default factory used to generate exceptions when injecting faults and no rule-specific factory is provided.

- **Type**: `Func<Exception>?`
- **Access**: Read-write
- **Thread Safety**: Safe for concurrent reads; writes are not synchronized and should be performed during initialization or with external synchronization.

---
### `public TimeSpan? LatencyDelay`

Gets or sets the default delay applied when injecting latency and no rule-specific delay is provided.

- **Type**: `TimeSpan?`
- **Access**: Read-write
- **Thread Safety**: Safe for concurrent reads; writes are not synchronized and should be performed during initialization or with external synchronization.

---
### `public TimeSpan? TimeoutDuration`

Gets or sets the default timeout duration applied when injecting timeouts and no rule-specific duration is provided.

- **Type**: `TimeSpan?`
- **Access**: Read-write
- **Thread Safety**: Safe for concurrent reads; writes are not synchronized and should be performed during initialization or with external synchronization.

---
### `public long InjectionsPerformed`

Gets the total number of fault injections that have occurred since the service was created, including those that were suppressed due to rule conditions not being met.

- **Type**: `long`
- **Access**: Read-only
- **Thread Safety**: Safe for concurrent reads.

---
### `public string RuleKey`

Gets the unique key identifying this rule within the service.

- **Type**: `string`
- **Access**: Read-only
- **Thread Safety**: Safe for concurrent reads.

---
### `public InjectedFaultException`

Exception thrown when a fault is injected by a matching rule.

- **Type**: `InjectedFaultException`
- **Access**: Public class
- **Thread Safety**: Instances are immutable after construction.

## Usage

### Example 1: Injecting a transient exception
