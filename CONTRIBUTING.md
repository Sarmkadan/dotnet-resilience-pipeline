# Contributing to DotNet Resilience Pipeline

Thanks for contributing! This guide covers setup, conventions, and the PR process.

## Prerequisites

- **.NET 10 SDK** (`net10.0`, set by `TargetFramework` in `DotNetResiliencePipeline.csproj`). There is no `global.json`; CI uses `10.0.x`.
- Git.

## Build

The repo root contains both `dotnet-resilience-pipeline.sln` and `DotNetResiliencePipeline.csproj`, so always pass the solution explicitly. A bare `dotnet build` or `dotnet test` fails with `MSB1011`.

```bash
dotnet restore dotnet-resilience-pipeline.sln
dotnet build dotnet-resilience-pipeline.sln                 # Debug
dotnet build dotnet-resilience-pipeline.sln -c Release      # what CI runs
```

## Test

```bash
dotnet test dotnet-resilience-pipeline.sln                  # full suite
dotnet test tests/dotnet-resilience-pipeline.Tests/dotnet-resilience-pipeline.Tests.csproj
dotnet test tests/dotnet-resilience-pipeline.Tests/dotnet-resilience-pipeline.Tests.csproj \
    --filter "FullyQualifiedName~CircuitBreaker"           # subset
```

- The test project is `tests/dotnet-resilience-pipeline.Tests/` (xUnit, FluentAssertions, Moq).
- Benchmarks live in `benchmarks/dotnet-resilience-pipeline.Benchmarks.csproj` (BenchmarkDotNet). Run them with `dotnet run -c Release --project benchmarks/dotnet-resilience-pipeline.Benchmarks.csproj`.

## Coding Conventions

- Follow `.editorconfig`: 4-space indentation, Allman braces, and `var` only when the type is apparent from the right-hand side.
- Every `.cs` file starts with `#nullable enable` and the author header block. Keep the header intact.
- Nullable reference types and implicit usings are enabled. Don't add new compiler warnings.
- Public types and members need XML doc comments (`GenerateDocumentationFile` is on).
- Async methods end in `Async`.
- Services are `sealed`. Put magic numbers in `const`/`static readonly` fields or `src/Constants/ResiliencyConstants.cs`.
- Thread safety: use `lock`, `Interlocked`, or `SemaphoreSlim`, and document guarantees on public types.
- Policies are paired by name: `XPolicy` in `src/Domain/Policies/` with `XService` in `src/Services/`.
- A new public type needs a matching `docs/<Type>.md`.

## Tests

- One `<Type>Tests.cs` per type in the flat `tests/dotnet-resilience-pipeline.Tests/` folder.
- Name methods `Method_Scenario_ExpectedResult` and assert with FluentAssertions `.Should()`.
- New policies need concurrency tests.

## Pull Requests

1. **Fork** the repository if you don't have push access, then **branch from an up-to-date `main`**: `git switch -c feat/short-description`.
2. **Keep PRs small and focused**: one feature or fix per PR.
3. **Add tests for behavior changes.** A bug fix should include a test that fails without the fix.
4. **Use conventional commit prefixes**: `feat:`, `fix:`, `docs:`.
5. **Build and test locally** before pushing. **CI (`.github/workflows/ci.yml`) must be green** before review.
6. **Describe the change in the PR body**: what changed, why, and how you tested it. Link issues with `Fixes #N`.

## Reporting Issues

Open a GitHub issue. For bugs, include:

- Output of `dotnet --version`
- Minimal reproduction steps
- Expected vs. actual behavior

## Code of Conduct and License

Please follow the [Code of Conduct](CODE_OF_CONDUCT.md). By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE).
