# DependencyInjectionExtensions

Provides extension methods for registering resilience pipelines and policies with the Microsoft.Extensions.DependencyInjection container, enabling integration of resilience patterns such as retries, timeouts, and fallbacks in dependency injection scenarios.

## API

### `AddResiliencePipeline`

Registers a resilience pipeline with the dependency injection container using a delegate-based configuration.
