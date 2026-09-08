# Benchmarks Overview

This document provides an overview of the benchmark suite in the `dotnet-resilience-pipeline.Benchmarks` project. It explains how to run the benchmarks and lists each benchmark class along with the scenarios it measures. For detailed documentation of each benchmark class, see the corresponding Markdown files in the `docs/` directory.

## Running the Benchmarks

The benchmarks are implemented using BenchmarkDotNet. To run them in Release mode:

```bash
dotnet run -c Release --project benchmarks/dotnet-resilience-pipeline.Benchmarks.csproj
```

You can also run a specific benchmark class:

```bash
dotnet run -c Release --project benchmarks/dotnet-resilience-pipeline.Benchmarks.csproj --filter *RetryBenchmarks*
```

## Benchmark Classes

| Benchmark Class | Scenarios Measured | Documentation |
|-----------------|--------------------|---------------|
| `RetryBenchmarks` | • Successful execution in closed state<br>• Failure recording and retry attempts<br>• State transition from success to failure<br>• Timeout handling within retries | [RetryBenchmarks.md](RetryBenchmarks.md) |
| `CircuitBreakerBenchmarks` | • Closed state execution<br>• Half‑Open state execution<br>• Open state short‑circuit<br>• Failure recording<br>• State transition latency | [CircuitBreakerBenchmarks.md](CircuitBreakerBenchmarks.md) |
| `BulkheadBenchmarks` | • Successful execution under capacity<br>• Rejection when capacity exceeded<br>• Queueing behavior<br>• State transition between active and idle | [BulkheadBenchmarks.md](BulkheadBenchmarks.md) |
| `TimeoutBenchmarks` | • Successful timeout handling<br>• Failure recording on timeout<br>• State transition after timeout threshold | [TimeoutBenchmarks.md](TimeoutBenchmarks.md) |
| `FallbackBenchmarks` | • Successful fallback execution<br>• Failure recording and fallback trigger<br>• State transition after fallback | [FallbackBenchmarks.md](FallbackBenchmarks.md) |
| `PolicyHealthBenchmarks` | • Health calculation under varying success rates<br>• Transition between healthy/unhealthy states | [PolicyHealthBenchmarks.md](PolicyHealthBenchmarks.md) |

> **Note**: The method names are derived from the `[Benchmark]` attributes in each class. If new benchmark methods are added, update this table accordingly.

## Extending the Benchmarks

If you add new benchmark classes or methods, remember to:

1. Add a new row to this table with the class name and a brief list of scenarios.
2. Create a dedicated Markdown file in `docs/` with detailed documentation for the new class.
3. Ensure the benchmark class is included in the project file (`dotnet-resilience-pipeline.Benchmarks.csproj`).

---
