# Contributing to DotNet Resilience Pipeline

Thank you for considering contributing to the DotNet Resilience Pipeline! This document provides guidelines and instructions for contributing.

## Code of Conduct

### Our Pledge

We are committed to providing a welcoming and inspiring community for all.

### Expected Behavior

- Use welcoming and inclusive language
- Be respectful of differing opinions, experiences, and identities
- Focus on what is best for the community
- Show empathy towards other community members

### Unacceptable Behavior

- Harassment of any kind
- Offensive comments
- Deliberate intimidation
- Other conduct which could reasonably be considered inappropriate

## Getting Started

### Prerequisites

- .NET 10.0 SDK or later
- Git
- Basic understanding of resilience patterns
- Familiarity with async/await in C#

### Setup Development Environment

```bash
# Clone the repository
git clone https://github.com/sarmkadan/dotnet-resilience-pipeline.git
cd dotnet-resilience-pipeline

# Restore dependencies
dotnet restore

# Build project
dotnet build

# Run tests
dotnet test
```

## Types of Contributions

### Bug Reports

Found a bug? Please report it by creating a GitHub issue with:
- Clear, descriptive title
- Detailed description of the problem
- Steps to reproduce
- Expected vs actual behavior
- Environment details (OS, .NET version, library version)
- Screenshots or code samples if applicable

### Feature Requests

Have a feature idea? Please:
- Check existing issues to avoid duplicates
- Clearly describe the feature and use case
- Explain why this feature would be useful
- Provide examples if applicable

### Documentation

Improvements to documentation are always welcome:
- Fixing typos or unclear explanations
- Adding examples or use cases
- Clarifying API documentation
- Improving code comments

### Code Contributions

## Coding Standards

### C# Style Guidelines

```csharp
// 1. Use meaningful names
public class CircuitBreakerPolicy  // ✓ Clear purpose
{
    public int FailureThreshold { get; set; }  // ✓ Clear intent
}

// 2. Proper spacing and formatting
public Task<T> ExecuteAsync<T>(
    Func<CancellationToken, Task<T>> operation,
    CancellationToken cancellationToken = default)
{
    // Implementation
}

// 3. Use latest C# features
// ✓ Good - Using records
public record PolicyResult<T>(bool IsSuccess, T? Value, Exception? Error);

// ✓ Good - Using nullable reference types
public string? OptionalValue { get; set; }

// 4. Comments only for non-obvious logic
private void ComplexLogic()
{
    // Only explain WHY, not WHAT
    // This ensures atomicity under high concurrency
    lock (syncLock)
    {
        // Implementation
    }
}

// 5. Consistent indentation (4 spaces, no tabs)
if (condition)
{
    DoSomething();
}
```

### File Header

Every C# file must include this header:

```csharp
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetResiliencePipeline;
```

### Naming Conventions

- `PascalCase` for public types and methods
- `camelCase` for local variables and parameters
- `_camelCase` for private fields
- `UPPER_SNAKE_CASE` for constants
- Descriptive names that explain intent

### Thread Safety

All public APIs must be thread-safe:

```csharp
public class ThreadSafeClass
{
    private readonly object _syncLock = new();
    
    public void SafeMethod()
    {
        lock (_syncLock)
        {
            // Thread-safe implementation
        }
    }
}
```

### Error Handling

```csharp
// ✓ Good - Specific exceptions
try
{
    await operation.ExecuteAsync();
}
catch (CircuitBreakerOpenException ex)
{
    logger.LogWarning("Circuit breaker is open", ex);
    throw;
}

// ✗ Bad - Generic catch-all
catch (Exception ex)
{
    // Don't swallow exceptions
}
```

## Pull Request Process

### Before You Start

1. **Check existing issues/PRs** - Avoid duplicate work
2. **Create an issue first** - Discuss major changes before implementing
3. **Create a feature branch** - Use descriptive name: `feature/circuit-breaker-improvements`

### Development Workflow

```bash
# Create feature branch
git checkout -b feature/your-feature-name

# Make changes
# ... edit files ...

# Format code
dotnet format

# Run tests locally
dotnet test

# Commit with clear message
git commit -m "feat: Add distributed circuit breaker support"

# Push to your fork
git push origin feature/your-feature-name
```

### Pull Request Guidelines

1. **Title**: Concise, descriptive (e.g., "Add circuit breaker diagnostics")
2. **Description**: 
   - What does this PR do?
   - Why is this change needed?
   - How was it tested?
3. **Testing**: Include test cases
4. **Documentation**: Update relevant docs
5. **No breaking changes** (unless major version bump)

### Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: Add distributed circuit breaker support
^--^  ^------------------------------------
|     |
|     +-> Description of change
|
+-------> Type: feat, fix, docs, style, refactor, test, chore
```

Examples:
- `feat: Add exponential backoff to retry policy`
- `fix: Resolve circuit breaker stuck in half-open state`
- `docs: Update deployment guide for Kubernetes`
- `test: Add thread safety tests for bulkhead`

## Testing Guidelines

### Unit Tests

All new code must include unit tests:

```csharp
[Fact]
public async Task ExecuteAsync_WithCircuitBreakerOpen_ThrowsException()
{
    // Arrange
    var policy = new CircuitBreakerPolicy { FailureThreshold = 1 };
    var service = new CircuitBreakerService(policy);
    
    // Act & Assert
    await Assert.ThrowsAsync<CircuitBreakerOpenException>(
        () => service.ExecuteAsync(async ct => true, CancellationToken.None)
    );
}
```

### Test Coverage

- Minimum 80% code coverage
- Test happy paths and error cases
- Include thread-safety tests for concurrent code
- Test edge cases and boundary conditions

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test
dotnet test --filter TestMethod=ExecuteAsync_WithCircuitBreakerOpen_ThrowsException
```

## Documentation Standards

### Code Documentation

```csharp
/// <summary>
/// Executes an operation with resilience policies.
/// </summary>
/// <typeparam name="T">The operation result type</typeparam>
/// <param name="operation">The operation to execute</param>
/// <param name="circuitBreaker">Optional circuit breaker policy</param>
/// <param name="retry">Optional retry policy</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>The execution result</returns>
/// <exception cref="CircuitBreakerOpenException">If circuit breaker is open</exception>
public Task<PolicyResult<T>> ExecuteAsync<T>(
    Func<CancellationToken, Task<T>> operation,
    CircuitBreakerPolicy? circuitBreaker = null,
    RetryPolicy? retry = null,
    CancellationToken cancellationToken = default)
```

### README Updates

When adding features, update README.md:
- Add to features list
- Add usage example
- Update table of contents if needed

### Documentation Files

Update relevant docs in `docs/`:
- `api-reference.md` - For new public APIs
- `architecture.md` - For architectural changes
- `faq.md` - For common questions

## Performance Considerations

### Benchmarking

For performance-critical code:

```csharp
[MemoryDiagnoser]
public class CircuitBreakerBenchmarks
{
    [Benchmark]
    public void CheckCircuitBreakerState()
    {
        // Benchmark implementation
    }
}
```

### Guidelines

- <100ns overhead for circuit breaker check
- <1ms overhead for timeout enforcement
- <500ns for bulkhead slot operations
- Minimal memory allocations

## Security Considerations

### Input Validation

```csharp
public class CircuitBreakerPolicy
{
    private int _failureThreshold = 5;
    
    public int FailureThreshold
    {
        get => _failureThreshold;
        set
        {
            if (value < 1)
                throw new ArgumentException("Must be >= 1");
            _failureThreshold = value;
        }
    }
}
```

### Sensitive Data

Don't log:
- Connection strings
- API keys or tokens
- User credentials
- Personal information

### Null Safety

Enable nullable reference types and use null-coalescing:

```csharp
public class Service
{
    private readonly ILogger? _logger;
    
    public Service(ILogger? logger = null)
    {
        _logger = logger;
    }
    
    public void Log(string message)
    {
        _logger?.LogInformation(message);
    }
}
```

## Review Process

### What Reviewers Look For

- ✓ Code follows standards
- ✓ Tests included and passing
- ✓ Documentation updated
- ✓ Thread safety verified
- ✓ No breaking changes
- ✓ Performance acceptable

### Feedback Response

- Respond to all comments
- Request clarification if needed
- Implement suggestions or explain reasoning
- Re-request review after changes

## Community

### Communication

- GitHub Issues: For bug reports and feature requests
- GitHub Discussions: For questions and ideas
- Respectful and constructive tone always

### Recognition

Contributors are recognized in:
- CHANGELOG.md
- GitHub contributors page
- Release notes

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

## Questions?

- Read `docs/faq.md` for common questions
- Check `docs/getting-started.md` for setup issues
- Open a GitHub discussion for general questions

## Additional Resources

- [Conventional Commits](https://www.conventionalcommits.org/)
- [.NET Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [C# Language Features](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/)

---

Thank you for helping make DotNet Resilience Pipeline better!

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**
