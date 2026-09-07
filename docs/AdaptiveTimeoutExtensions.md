# AdaptiveTimeoutExtensions

`AdaptiveTimeoutExtensions` provides `IServiceCollection` extension methods for registering adaptive-timeout components with Microsoft dependency injection. Both methods return the same service collection, so registration calls can be chained.

For details about the registered types, see [AdaptiveTimeoutPolicy](AdaptiveTimeoutPolicy.md) and [AdaptiveTimeoutService](AdaptiveTimeoutService.md).

## `AddAdaptiveTimeout(IServiceCollection)`

```csharp
public static IServiceCollection AddAdaptiveTimeout(
    this IServiceCollection services)
```

Registers `AdaptiveTimeoutService` with a singleton lifetime. The service is created by the container when it is first resolved and the same instance is returned for the lifetime of the service provider.

The method throws `ArgumentNullException` when `services` is `null`.

## `AddAdaptiveTimeout(IServiceCollection, string, TimeSpan, Action<AdaptiveTimeoutPolicy>?)`

```csharp
public static IServiceCollection AddAdaptiveTimeout(
    this IServiceCollection services,
    string policyName,
    TimeSpan initialTimeout,
    Action<AdaptiveTimeoutPolicy>? configure = null)
```

Registers the following concrete types:

| Service type | Lifetime | Registration behavior |
| --- | --- | --- |
| `AdaptiveTimeoutService` | Singleton | Registered by type and constructed by the dependency-injection container. |
| `AdaptiveTimeoutPolicy` | Singleton | Created lazily by a factory when the policy is first resolved. |

The policy factory creates an `AdaptiveTimeoutPolicy` named `policyName`, assigns `initialTimeout` to `InitialTimeout`, and then invokes the optional `configure` delegate. It calls `ResetStatistics()` afterward so that `CurrentTimeout` reflects the final configured `InitialTimeout` value.

When the policy singleton is created, the factory also tries to resolve `ResiliencyPipelineService`. If a pipeline is registered, the policy is registered with it; if no pipeline is registered, policy creation still succeeds. Because this wiring occurs in the policy factory, resolving only `AdaptiveTimeoutService` does not force creation or pipeline registration of the policy.

The method throws:

- `ArgumentNullException` when `services` is `null`.
- `ArgumentException` when `policyName` is `null`, empty, or whitespace.
- `ArgumentOutOfRangeException` when `initialTimeout` is zero or negative.

## Registration and resolution

`AdaptiveTimeoutService` requires `ILogger<AdaptiveTimeoutService>`, so the example also registers logging:

```csharp
using DotNetResiliencePipeline.Configuration;
using DotNetResiliencePipeline.Services;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddLogging();
services.AddAdaptiveTimeout();

using var provider = services.BuildServiceProvider();
var adaptiveTimeout = provider.GetRequiredService<AdaptiveTimeoutService>();
```

To register and resolve a configured policy as well:

```csharp
using DotNetResiliencePipeline.Configuration;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddLogging();
services.AddAdaptiveTimeout(
    "payment-api",
    TimeSpan.FromSeconds(5),
    policy =>
    {
        policy.MinTimeout = TimeSpan.FromSeconds(1);
        policy.MaxTimeout = TimeSpan.FromSeconds(20);
        policy.TargetPercentile = 95;
    });

using var provider = services.BuildServiceProvider();
var adaptiveTimeout = provider.GetRequiredService<AdaptiveTimeoutService>();
var policy = provider.GetRequiredService<AdaptiveTimeoutPolicy>();
```

Calling either overload more than once adds another service descriptor; it does not replace or deduplicate earlier registrations.
