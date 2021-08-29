# Contributing to DotNet Resilience Pipeline

Thank you for your interest in contributing!

## Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

## Building Locally

```bash
# Clone your fork
git clone https://github.com/your-username/dotnet-resilience-pipeline.git
cd dotnet-resilience-pipeline

# Restore dependencies
dotnet restore

# Build in release configuration
dotnet build --configuration Release

# Build in debug (default)
dotnet build
```

## Running Tests

```bash
# Run the full test suite
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run a specific test project
dotnet test tests/dotnet-resilience-pipeline.Tests/

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Workflow

1. **Fork** the repository on GitHub.
2. **Create a branch**: `git checkout -b feature/your-feature-name`
3. **Make your changes** with tests and documentation.
4. **Ensure all tests pass**: `dotnet test`
5. **Push** your branch and open a Pull Request against `main`.

## Pull Request Guidelines

- Keep PRs focused — one feature or fix per PR.
- Provide a clear description of what changed and why.
- Reference any related issues with `Fixes #N` or `Closes #N`.
- Ensure CI passes before requesting review.
- All public API changes require XML documentation comments.

## Code Style

- Follow the `.editorconfig` settings in the repository root.
- Use 4 spaces for indentation in C# files.
- Place opening braces on new lines (Allman style).
- Prefer `var` only when the type is apparent from the right-hand side.
- Write self-documenting code; add comments only where logic needs clarification.
- Keep all existing author and copyright headers intact.

## Thread Safety

- Use proper synchronization primitives (`lock`, `Interlocked`, `SemaphoreSlim`).
- Document any thread-safety guarantees or assumptions on public types.
- Add concurrency tests for new policy implementations.

## Reporting Issues

Open an issue using GitHub Issues. For bugs, include:
- .NET version (`dotnet --version`)
- Minimal reproduction steps
- Expected vs actual behavior

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
