#nullable enable
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Middleware;
using FluentAssertions;
using System;
using Xunit;

/// <summary>
/// Tests for ErrorHandlingMiddlewareExtensions extension methods.
/// </summary>
public sealed class ErrorHandlingMiddlewareExtensionsTests
{
    /// <summary>
    /// Creates a new ErrorHandlingMiddleware instance for testing.
    /// </summary>
    private static ErrorHandlingMiddleware CreateMiddleware()
    {
        var middleware = new ErrorHandlingMiddleware();

        // Add some test error contexts
        middleware.HandleException(new TimeoutException("Test timeout"), "TimeoutPolicy", "OperationA");
        middleware.HandleException(new InvalidOperationException("Invalid operation"), "ValidationPolicy", "OperationB");
        middleware.HandleException(new TimeoutException("Another timeout"), "TimeoutPolicy", "OperationC");
        middleware.HandleException(new CircuitBreakerOpenException("CircuitBreakerPolicy", TimeSpan.FromSeconds(30), 5), "CircuitBreakerPolicy", "OperationD");

        return middleware;
    }

    /// <summary>
    /// Tests GetErrorsByType with valid exception type.
    /// </summary>
    [Fact]
    public void GetErrorsByType_WithValidType_ReturnsMatchingErrors()
    {
        // Arrange
        var middleware = CreateMiddleware();

        // Act
        var timeoutErrors = middleware.GetErrorsByType("TimeoutException");
        var invalidOpErrors = middleware.GetErrorsByType("InvalidOperationException");
        var circuitBreakerErrors = middleware.GetErrorsByType("CircuitBreakerOpenException");

        // Assert
        timeoutErrors.Should().HaveCount(2);
        timeoutErrors.Should().AllBeOfType<ErrorContext>();
        timeoutErrors.Should().AllSatisfy(c => c.ExceptionType.Should().Be("TimeoutException"));

        invalidOpErrors.Should().HaveCount(1);
        invalidOpErrors[0].ExceptionType.Should().Be("InvalidOperationException");

        circuitBreakerErrors.Should().HaveCount(1);
        circuitBreakerErrors[0].ExceptionType.Should().Be("CircuitBreakerOpenException");
    }

    /// <summary>
    /// Tests GetErrorsByType with empty result.
    /// </summary>
    [Fact]
    public void GetErrorsByType_WithNonExistentType_ReturnsEmptyList()
    {
        // Arrange
        var middleware = CreateMiddleware();

        // Act
        var errors = middleware.GetErrorsByType("NonExistentException");

        // Assert
        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests GetErrorsByType with null middleware throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void GetErrorsByType_WithNullMiddleware_ThrowsArgumentNullException()
    {
        // Arrange
        ErrorHandlingMiddleware? middleware = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => middleware!.GetErrorsByType("TimeoutException"));
    }

    /// <summary>
    /// Tests GetErrorsByType with null exception type throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void GetErrorsByType_WithNullExceptionType_ThrowsArgumentNullException()
    {
        // Arrange
        var middleware = CreateMiddleware();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => middleware.GetErrorsByType(null!));
    }

    /// <summary>
    /// Tests GetErrorsByType with whitespace exception type throws ArgumentException.
    /// </summary>
    [Fact]
    public void GetErrorsByType_WithWhitespaceExceptionType_ThrowsArgumentException()
    {
        // Arrange
        var middleware = CreateMiddleware();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => middleware.GetErrorsByType("   "));
    }

    /// <summary>
    /// Tests GetErrorsByRecoverability with recoverableOnly=true.
    /// </summary>
    [Fact]
    public void GetErrorsByRecoverability_WithRecoverableOnly_ReturnsRecoverableErrors()
    {
        // Arrange
        var middleware = CreateMiddleware();

        // Act
        var recoverableErrors = middleware.GetErrorsByRecoverability(true);
        var nonRecoverableErrors = middleware.GetErrorsByRecoverability(false);

        // Assert
        recoverableErrors.Should().HaveCount(3); // TimeoutException, TimeoutException, CircuitBreakerOpenException
        recoverableErrors.Should().AllSatisfy(c => c.IsRecoverable.Should().BeTrue());

        nonRecoverableErrors.Should().HaveCount(1); // InvalidOperationException
        nonRecoverableErrors[0].IsRecoverable.Should().BeFalse();
    }

    /// <summary>
    /// Tests GetErrorsByRecoverability with default parameter (recoverableOnly=true).
    /// </summary>
    [Fact]
    public void GetErrorsByRecoverability_WithDefaultParameter_ReturnsRecoverableErrors()
    {
        // Arrange
        var middleware = CreateMiddleware();

        // Act
        var errors = middleware.GetErrorsByRecoverability();

        // Assert
        errors.Should().HaveCount(3);
        errors.Should().AllSatisfy(c => c.IsRecoverable.Should().BeTrue());
    }

    /// <summary>
    /// Tests GetErrorsByRecoverability with null middleware throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void GetErrorsByRecoverability_WithNullMiddleware_ThrowsArgumentNullException()
    {
        // Arrange
        ErrorHandlingMiddleware? middleware = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => middleware!.GetErrorsByRecoverability());
    }

    /// <summary>
    /// Tests GetErrorsForOperation with valid operation name.
    /// </summary>
    [Fact]
    public void GetErrorsForOperation_WithValidOperationName_ReturnsMatchingErrors()
    {
        // Arrange
        var middleware = CreateMiddleware();

        // Act
        var opAErrors = middleware.GetErrorsForOperation("OperationA");
        var opBErrors = middleware.GetErrorsForOperation("OperationB");
        var opCErrors = middleware.GetErrorsForOperation("OperationC");
        var opDErrors = middleware.GetErrorsForOperation("OperationD");

        // Assert
        opAErrors.Should().HaveCount(1);
        opAErrors[0].OperationName.Should().Be("OperationA");

        opBErrors.Should().HaveCount(1);
        opBErrors[0].OperationName.Should().Be("OperationB");

        opCErrors.Should().HaveCount(1);
        opCErrors[0].OperationName.Should().Be("OperationC");

        opDErrors.Should().HaveCount(1);
        opDErrors[0].OperationName.Should().Be("OperationD");
    }

    /// <summary>
    /// Tests GetErrorsForOperation with empty result.
    /// </summary>
    [Fact]
    public void GetErrorsForOperation_WithNonExistentOperation_ReturnsEmptyList()
    {
        // Arrange
        var middleware = CreateMiddleware();

        // Act
        var errors = middleware.GetErrorsForOperation("NonExistentOperation");

        // Assert
        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests GetErrorsForOperation with null middleware throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void GetErrorsForOperation_WithNullMiddleware_ThrowsArgumentNullException()
    {
        // Arrange
        ErrorHandlingMiddleware? middleware = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => middleware!.GetErrorsForOperation("OperationA"));
    }

    /// <summary>
    /// Tests GetErrorsForOperation with null operation name throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void GetErrorsForOperation_WithNullOperationName_ThrowsArgumentNullException()
    {
        // Arrange
        var middleware = CreateMiddleware();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => middleware.GetErrorsForOperation(null!));
    }

    /// <summary>
    /// Tests GetErrorsForOperation with whitespace operation name throws ArgumentException.
    /// </summary>
    [Fact]
    public void GetErrorsForOperation_WithWhitespaceOperationName_ThrowsArgumentException()
    {
        // Arrange
        var middleware = CreateMiddleware();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => middleware.GetErrorsForOperation("   "));
    }

    /// <summary>
    /// Tests GenerateErrorReport with includeContexts=false.
    /// </summary>
    [Fact]
    public void GenerateErrorReport_WithoutContexts_ReturnsFormattedReport()
    {
        // Arrange
        var middleware = CreateMiddleware();

        // Act
        var report = middleware.GenerateErrorReport(includeContexts: false);

        // Assert
        report.Should().NotBeNullOrEmpty();
        report.Should().Contain("=== Error Handling Middleware Report ===");
        report.Should().Contain("Total unique error types:");
        report.Should().Contain("Total error occurrences:");
        report.Should().Contain("Most Common Errors");
        report.Should().NotContain("Recent Error Contexts");
    }

    /// <summary>
    /// Tests GenerateErrorReport with includeContexts=true.
    /// </summary>
    [Fact]
    public void GenerateErrorReport_WithContexts_ReturnsReportWithContexts()
    {
        // Arrange
        var middleware = CreateMiddleware();

        // Act
        var report = middleware.GenerateErrorReport(includeContexts: true);

        // Assert
        report.Should().NotBeNullOrEmpty();
        report.Should().Contain("=== Error Handling Middleware Report ===");
        report.Should().Contain("Recent Error Contexts");
        report.Should().Contain("TimeoutException");
        report.Should().Contain("InvalidOperationException");
    }

    /// <summary>
    /// Tests GenerateErrorReport with null middleware throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void GenerateErrorReport_WithNullMiddleware_ThrowsArgumentNullException()
    {
        // Arrange
        ErrorHandlingMiddleware? middleware = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => middleware!.GenerateErrorReport());
    }

    /// <summary>
    /// Tests GenerateErrorReport with empty error contexts.
    /// </summary>
    [Fact]
    public void GenerateErrorReport_WithEmptyContexts_ReturnsDefaultReport()
    {
        // Arrange
        var middleware = new ErrorHandlingMiddleware();

        // Act
        var report = middleware.GenerateErrorReport();

        // Assert
        report.Should().NotBeNullOrEmpty();
        report.Should().Contain("=== Error Handling Middleware Report ===");
        report.Should().Contain("Total unique error types: 0");
        report.Should().Contain("Total error occurrences: 0");
    }

    /// <summary>
    /// Tests HasErrorOccurredRecently with recent error.
    /// </summary>
    [Fact]
    public void HasErrorOccurredRecently_WithRecentError_ReturnsTrue()
    {
        // Arrange
        var middleware = CreateMiddleware();

        // Act
        var hasTimeoutError = middleware.HasErrorOccurredRecently("TimeoutException", timeWindowMinutes: 60);
        var hasInvalidOpError = middleware.HasErrorOccurredRecently("InvalidOperationException", timeWindowMinutes: 60);

        // Assert
        hasTimeoutError.Should().BeTrue();
        hasInvalidOpError.Should().BeTrue();
    }

    /// <summary>
    /// Tests HasErrorOccurredRecently with errors that occurred in the past.
    /// </summary>
    [Fact]
    public void HasErrorOccurredRecently_WithRecentErrors_ReturnsTrue()
    {
        // Arrange
        var middleware = CreateMiddleware();

        // Act - errors were just created, so they should be recent within a reasonable time window
        var hasTimeoutError = middleware.HasErrorOccurredRecently("TimeoutException", timeWindowMinutes: 60);
        var hasInvalidOpError = middleware.HasErrorOccurredRecently("InvalidOperationException", timeWindowMinutes: 60);

        // Assert
        hasTimeoutError.Should().BeTrue();
        hasInvalidOpError.Should().BeTrue();
    }

    /// <summary>
    /// Tests HasErrorOccurredRecently with null middleware throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void HasErrorOccurredRecently_WithNullMiddleware_ThrowsArgumentNullException()
    {
        // Arrange
        ErrorHandlingMiddleware? middleware = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => middleware!.HasErrorOccurredRecently("TimeoutException"));
    }

    /// <summary>
    /// Tests HasErrorOccurredRecently with null exception type throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void HasErrorOccurredRecently_WithNullExceptionType_ThrowsArgumentNullException()
    {
        // Arrange
        var middleware = CreateMiddleware();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => middleware.HasErrorOccurredRecently(null!));
    }

    /// <summary>
    /// Tests HasErrorOccurredRecently with whitespace exception type throws ArgumentException.
    /// </summary>
    [Fact]
    public void HasErrorOccurredRecently_WithWhitespaceExceptionType_ThrowsArgumentException()
    {
        // Arrange
        var middleware = CreateMiddleware();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => middleware.HasErrorOccurredRecently("   "));
    }

    /// <summary>
    /// Tests HasErrorOccurredRecently with zero time window throws ArgumentOutOfRangeException.
    /// </summary>
    [Fact]
    public void HasErrorOccurredRecently_WithZeroTimeWindow_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var middleware = CreateMiddleware();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => middleware.HasErrorOccurredRecently("TimeoutException", 0));
    }

    /// <summary>
    /// Tests HasErrorOccurredRecently with negative time window throws ArgumentOutOfRangeException.
    /// </summary>
    [Fact]
    public void HasErrorOccurredRecently_WithNegativeTimeWindow_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var middleware = CreateMiddleware();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => middleware.HasErrorOccurredRecently("TimeoutException", -10));
    }

    /// <summary>
    /// Tests GetTotalErrorCount with multiple errors.
    /// </summary>
    [Fact]
    public void GetTotalErrorCount_WithMultipleErrors_ReturnsTotalCount()
    {
        // Arrange
        var middleware = CreateMiddleware();

        // Act
        var totalCount = middleware.GetTotalErrorCount();

        // Assert
        totalCount.Should().Be(4); // 4 errors added in CreateMiddleware
    }

    /// <summary>
    /// Tests GetTotalErrorCount with empty error contexts.
    /// </summary>
    [Fact]
    public void GetTotalErrorCount_WithEmptyContexts_ReturnsZero()
    {
        // Arrange
        var middleware = new ErrorHandlingMiddleware();

        // Act
        var totalCount = middleware.GetTotalErrorCount();

        // Assert
        totalCount.Should().Be(0);
    }

    /// <summary>
    /// Tests GetTotalErrorCount with null middleware throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void GetTotalErrorCount_WithNullMiddleware_ThrowsArgumentNullException()
    {
        // Arrange
        ErrorHandlingMiddleware? middleware = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => middleware!.GetTotalErrorCount());
    }

    /// <summary>
    /// Tests all methods work correctly with empty middleware.
    /// </summary>
    [Fact]
    public void AllMethods_WithEmptyMiddleware_WorkCorrectly()
    {
        // Arrange
        var middleware = new ErrorHandlingMiddleware();

        // Act & Assert - all should not throw and return sensible defaults
        var byType = middleware.GetErrorsByType("AnyException");
        byType.Should().BeEmpty();

        var byRecoverability = middleware.GetErrorsByRecoverability();
        byRecoverability.Should().BeEmpty();

        var forOperation = middleware.GetErrorsForOperation("AnyOperation");
        forOperation.Should().BeEmpty();

        var report = middleware.GenerateErrorReport();
        report.Should().NotBeNullOrEmpty();

        var hasError = middleware.HasErrorOccurredRecently("AnyException", 60);
        hasError.Should().BeFalse();

        var totalCount = middleware.GetTotalErrorCount();
        totalCount.Should().Be(0);
    }
}