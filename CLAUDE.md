# CLAUDE.md

.NET 10 library + demo console app implementing resilience patterns (retry, circuit breaker, timeout, bulkhead, fallback) behind a single orchestrator (`ResiliencyPipelineService`) with a fluent builder and DI integration.

## Build

```bash
dotnet restore
dotnet build                                   # Debug
dotnet build --configuration Release           # what CI runs
dotnet run --project DotNetResiliencePipeline.csproj   # run the demo (src/Program.cs)
dotnet pack -c Release -o ./nupkg              # NuGet package
make build | make run | make pack              # Makefile wrappers (Release)
```

Solution `dotnet-resilience-pipeline.sln` contains 3 projects: the library/demo (`DotNetResiliencePipeline.csproj`, root), tests, benchmarks. The root csproj compiles everything under `src/` and explicitly excludes `tests/**`, `benchmarks/**`, `examples/**`.

## Test

```bash
dotnet test                                                   # full suite
dotnet test tests/dotnet-resilience-pipeline.Tests/           # test project only
dotnet test --filter "FullyQualifiedName~CircuitBreaker"      # subset
dotnet test --collect:"XPlat Code Coverage"
make test                                                     # Release, --no-build (build first)
```

Stack: xUnit 2.9, FluentAssertions 8, Moq. Benchmarks: `dotnet run -c Release --project benchmarks/dotnet-resilience-pipeline.Benchmarks.csproj` (BenchmarkDotNet).

## Lint / format

```bash
dotnet format                                              # make format
dotnet build -c Release /p:EnforceCodeStyleInBuild=true    # make lint
```

Style is defined in `.editorconfig` (4 spaces, Allman braces, `csharp_new_line_before_open_brace = all`). `GenerateDocumentationFile` is on: public API needs XML doc comments or the build warns.

## Layout

- `src/Program.cs` - demo entry point (`OutputType=Exe`)
- `src/Services/` - policy executors: `ResiliencyPipelineService` (orchestrator), `RetryService`, `CircuitBreakerService`, `TimeoutService`, `BulkheadService`, `FallbackService`, `AdaptiveTimeoutService`, `FailureInjectionService`
- `src/Domain/Policies/` - policy configuration types (`ResiliencyPolicy` base, `RetryPolicy`, `CircuitBreakerPolicy`, `TimeoutPolicy`, `AdaptiveTimeoutPolicy`, `BulkheadPolicy`, `FallbackPolicy`)
- `src/Domain/` - `PolicyResult`, `IPipelineMetrics`
- `src/Configuration/` - `DependencyInjectionExtensions.AddResiliencePipeline`, `ResiliencyPipelineBuilder`, options + validator
- `src/Exceptions/` - `ResiliencyExceptions.cs` (CircuitBreakerOpen, BulkheadRejected, OperationTimeout, MaxRetriesExceeded, FallbackFailed, ...)
- `src/Data/` - in-memory repositories (execution history, policies)
- `src/Events/`, `src/Workers/`, `src/Middleware/`, `src/Api/Controllers/`, `src/Integration/`, `src/Cli/`, `src/Formatters/`, `src/Caching/`, `src/Utilities/`, `src/Constants/`
- `tests/dotnet-resilience-pipeline.Tests/` - flat, one `*Tests.cs` per type
- `benchmarks/` - BenchmarkDotNet project
- `examples/` - standalone `.cs` samples, not compiled by any project
- `docs/` - one markdown file per public type plus `architecture.md`, `faq.md`, `benchmarks.md`; `QUICK_REFERENCE.md` is the API cheat sheet
- `.github/workflows/` - build/ci/codeql/docker/nuget-publish/release
- `Dockerfile`, `docker-compose.yml`, `prometheus.yml` - container/monitoring setup

## Conventions

- Namespace mirrors folder: `DotNetResiliencePipeline.<Folder>`; root namespace `DotNetResiliencePipeline`.
- Every `.cs` starts with `#nullable enable` and the author header block (`Author: Vladyslav Zaiets | https://sarmkadan.com`). Keep headers intact.
- Nullable + ImplicitUsings enabled; `LangVersion=latest`; services are `sealed`.
- Policy/service pairs: `XPolicy` (config, in Domain/Policies) + `XService` (execution, in Services). Policies are not applied implicitly; callers pass them to `ExecuteAsync`.
- Companion files by suffix: `*Extensions.cs` (extension methods), `*JsonExtensions.cs` (`ToJson`/`FromJson`/`TryFromJson`), `*Validation.cs` (validation helpers). New types follow the same split.
- Tests: `<Type>Tests.cs`, methods `Method_Scenario_ExpectedResult`, FluentAssertions `.Should()`.
- Magic numbers go into `private const`/`static readonly` fields (see `Program.cs`) or `Constants/ResiliencyConstants.cs`.
- Thread safety: `lock`, `Interlocked`, `SemaphoreSlim`; document guarantees on public types; add concurrency tests for new policies.
- Commits: conventional prefixes (`docs:`, `feat:`, `fix:`). Adding a public type means adding `docs/<Type>.md`.

## Gotchas

- Stray junk paths exist at repo root and under `tests/` (e.g. `}`, `CircuitBreakerHalfOpenBugTestsExtensions.RecordFailuresAndReopenCircuit(policy);`, a `tests/dotnet-resilience-paced...` directory). They are not part of the build; do not treat them as source.
- `.aider*` files are gitignored session artifacts.
