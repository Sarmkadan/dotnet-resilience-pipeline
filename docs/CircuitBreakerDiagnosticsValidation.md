# CircuitBreakerDiagnosticsValidation

Provides validation utilities for circuit breaker diagnostics configurations, ensuring that diagnostic settings are valid before they are applied to a resilience pipeline.

## API

### `Validate(IResilienceStrategy strategy)`

Validates the diagnostics configuration of the specified resilience strategy.

- **Parameters**
  - `strategy` – The resilience strategy whose diagnostics configuration is to be validated.
- **Return value**
  - An `IReadOnlyList<string>` containing validation error messages. If the list is empty, the configuration is valid.
- **Exceptions**
  - Throws `ArgumentNullException` if `strategy` is `null`.

### `Validate(ResilienceStrategyBuilder builder)`

Validates the diagnostics configuration of a resilience strategy builder.

- **Parameters**
  - `builder` – The resilience strategy builder whose diagnostics configuration is to be validated.
- **Return value**
  - An `IReadOnlyList<string>` containing validation error messages. If the list is empty, the configuration is valid.
- **Exceptions**
  - Throws `ArgumentNullException` if `builder` is `null`.

### `Validate(ResiliencePipeline pipeline)`

Validates the diagnostics configuration of a resilience pipeline.

- **Parameters**
  - `pipeline` – The resilience pipeline whose diagnostics configuration is to be validated.
- **Return value**
  - An `IReadOnlyList<string>` containing validation error messages. If the list is empty, the configuration is valid.
- **Exceptions**
  - Throws `ArgumentNullException` if `pipeline` is `null`.

### `IsValid(IResilienceStrategy strategy)`

Determines whether the diagnostics configuration of the specified resilience strategy is valid.

- **Parameters**
  - `strategy` – The resilience strategy whose diagnostics configuration is to be checked.
- **Return value**
  - `true` if the configuration is valid; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `strategy` is `null`.

### `IsValid(ResilienceStrategyBuilder builder)`

Determines whether the diagnostics configuration of a resilience strategy builder is valid.

- **Parameters**
  - `builder` – The resilience strategy builder whose diagnostics configuration is to be checked.
- **Return value**
  - `true` if the configuration is valid; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `builder` is `null`.

### `IsValid(ResiliencePipeline pipeline)`

Determines whether the diagnostics configuration of a resilience pipeline is valid.

- **Parameters**
  - `pipeline` – The resilience pipeline whose diagnostics configuration is to be checked.
- **Return value**
  - `true` if the configuration is valid; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `pipeline` is `null`.

### `EnsureValid(IResilienceStrategy strategy)`

Validates the diagnostics configuration of the specified resilience strategy and throws if invalid.

- **Parameters**
  - `strategy` – The resilience strategy whose diagnostics configuration is to be validated.
- **Exceptions**
  - Throws `ArgumentNullException` if `strategy` is `null`.
  - Throws `InvalidOperationException` if the diagnostics configuration is invalid.

### `EnsureValid(ResilienceStrategyBuilder builder)`

Validates the diagnostics configuration of a resilience strategy builder and throws if invalid.

- **Parameters**
  - `builder` – The resilience strategy builder whose diagnostics configuration is to be validated.
- **Exceptions**
  - Throws `ArgumentNullException` if `builder` is `null`.
  - Throws `InvalidOperationException` if the diagnostics configuration is invalid.

### `EnsureValid(ResiliencePipeline pipeline)`

Validates the diagnostics configuration of a resilience pipeline and throws if invalid.

- **Parameters**
  - `pipeline` – The resilience pipeline whose diagnostics configuration is to be validated.
- **Exceptions**
  - Throws `ArgumentNullException` if `pipeline` is `null`.
  - Throws `InvalidOperationException` if the diagnostics configuration is invalid.

## Usage
