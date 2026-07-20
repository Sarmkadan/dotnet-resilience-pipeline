#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Services;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

/// <summary>
/// Contains unit tests for the <see cref="FailureInjectionService"/> class.
/// Verifies behavior for scenarios like rule registration, exception injection,
/// latency injection, timeout injection, rule disabling, and rule removal.
/// </summary>
public sealed class FailureInjectionServiceTests
{
    /// <summary>
    /// Verifies that when no rule is registered, the service executes the operation normally.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_NoRuleRegistered_RunsOperationNormally()
    {
        var sut = new FailureInjectionService();

        var result = await sut.ExecuteAsync("missing", _ => Task.FromResult(42));

        result.Should().Be(42);
    }

    /// <summary>
    /// Verifies that an exception rule with 100% injection rate throws the configured <see cref="InjectedFaultException"/>.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ExceptionRule_ThrowsInjectedFault()
    {
        var sut = new FailureInjectionService();
        sut.AddRule(new InjectionRule
        {
            Key = "svc",
            Type = InjectionType.Exception,
            InjectionRate = 1.0,
            ExceptionMessage = "boom"
        });

        Func<Task> act = () => sut.ExecuteAsync("svc", _ => Task.FromResult(0));

        await act.Should().ThrowAsync<InjectedFaultException>()
            .WithMessage("boom");
    }

    /// <summary>
    /// Verifies that an exception rule with 100% injection rate throws the configured exception type.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ExceptionRule_ThrowsConfiguredExceptionType()
    {
        var sut = new FailureInjectionService();
        sut.AddRule(new InjectionRule
        {
            Key = "custom-ex",
            Type = InjectionType.Exception,
            InjectionRate = 1.0,
            ExceptionFactory = () => new InvalidOperationException("custom-factory-error")
        });

        Func<Task> act = () => sut.ExecuteAsync("custom-ex", _ => Task.FromResult(0));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("custom-factory-error");
    }

    /// <summary>
    /// Verifies that a latency rule adds the configured delay before returning the result.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_LatencyRule_AddsDelayAndReturnsResult()
    {
        var sut = new FailureInjectionService();
        sut.AddRule(new InjectionRule
        {
            Key = "latency-svc",
            Type = InjectionType.Latency,
            InjectionRate = 1.0,
            LatencyDelay = TimeSpan.FromMilliseconds(50)
        });

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await sut.ExecuteAsync("latency-svc", _ => Task.FromResult(99));
        sw.Stop();

        result.Should().Be(99);
        sw.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(40);
    }

    /// <summary>
    /// Verifies that a timeout rule simulates a hung operation that never completes.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_TimeoutRule_SimulatesHungOperation()
    {
        var sut = new FailureInjectionService();
        sut.AddRule(new InjectionRule
        {
            Key = "timeout-svc",
            Type = InjectionType.Timeout,
            InjectionRate = 1.0,
            TimeoutDuration = TimeSpan.FromMilliseconds(100)
        });

        Func<Task> act = () => sut.ExecuteAsync("timeout-svc", _ => Task.FromResult(42));

        await act.Should().ThrowAsync<OperationCanceledException>()
            .WithMessage("*Injected timeout after 0.1s for rule 'timeout-svc'*");
    }

    /// <summary>
    /// Verifies that an injection rule with 0% injection rate never injects faults.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ZeroInjectionRate_NeverInjects()
    {
        var sut = new FailureInjectionService();
        sut.AddRule(new InjectionRule
        {
            Key = "zero-rate",
            Type = InjectionType.Exception,
            InjectionRate = 0.0
        });

        // 10 calls – none should throw
        for (var i = 0; i < 10; i++)
        {
            var r = await sut.ExecuteAsync("zero-rate", _ => Task.FromResult(i));
            r.Should().Be(i);
        }
    }

    /// <summary>
    /// Verifies that an injection rule with 100% injection rate always injects faults.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_HundredPercentInjectionRate_AlwaysInjects()
    {
        var sut = new FailureInjectionService();
        sut.AddRule(new InjectionRule
        {
            Key = "hundred-percent",
            Type = InjectionType.Exception,
            InjectionRate = 1.0,
            ExceptionMessage = "100% failure"
        });

        // Multiple calls – all should throw
        for (var i = 0; i < 5; i++)
        {
            Func<Task> act = () => sut.ExecuteAsync("hundred-percent", _ => Task.FromResult(i));
            await act.Should().ThrowAsync<InjectedFaultException>();
        }
    }

    /// <summary>
    /// Verifies that a disabled rule does not affect the operation execution.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_DisabledRule_RunsOperationNormally()
    {
        var sut = new FailureInjectionService();
        sut.AddRule(new InjectionRule
        {
            Key = "disabled",
            Type = InjectionType.Exception,
            InjectionRate = 1.0,
            IsEnabled = false
        });

        var result = await sut.ExecuteAsync("disabled", _ => Task.FromResult(7));

        result.Should().Be(7);
    }

    /// <summary>
    /// Verifies that the <see cref="FailureInjectionService.TotalInjections"/> counter increments
    /// for each successful fault injection.
    /// </summary>
    [Fact]
    public async Task TotalInjections_IncrementsOnEachInjectedCall()
    {
        var sut = new FailureInjectionService();
        sut.AddRule(new InjectionRule
        {
            Key = "counter",
            Type = InjectionType.Exception,
            InjectionRate = 1.0
        });

        for (var i = 0; i < 3; i++)
        {
            try { await sut.ExecuteAsync("counter", _ => Task.FromResult(0)); }
            catch (InjectedFaultException) { /* expected */ }
        }

        sut.TotalInjections.Should().Be(3);
    }

    /// <summary>
    /// Verifies that a custom exception factory is used to create exceptions when configured.
    /// </summary>
    [Fact]
    public async Task ExceptionFactory_UsedWhenProvided()
    {
        var sut = new FailureInjectionService();
        sut.AddRule(new InjectionRule
        {
            Key = "custom-ex",
            Type = InjectionType.Exception,
            InjectionRate = 1.0,
            ExceptionFactory = () => new InvalidOperationException("custom-factory-error")
        });

        Func<Task> act = () => sut.ExecuteAsync("custom-ex", _ => Task.FromResult(0));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("custom-factory-error");
    }

    /// <summary>
    /// Verifies that <see cref="FailureInjectionService.DisableAll()"/> deactivates all registered rules.
    /// </summary>
    [Fact]
    public void DisableAll_DeactivatesAllRules()
    {
        var sut = new FailureInjectionService();
        sut.AddRule(new InjectionRule { Key = "r1", Type = InjectionType.Exception, InjectionRate = 1.0 });
        sut.AddRule(new InjectionRule { Key = "r2", Type = InjectionType.Exception, InjectionRate = 1.0 });

        sut.DisableAll();

        sut.GetRules().Should().AllSatisfy(r => r.IsEnabled.Should().BeFalse());
    }

    /// <summary>
    /// Verifies that <see cref="FailureInjectionService.RemoveRule(string)"/> removes a rule by key.
    /// </summary>
    [Fact]
    public void RemoveRule_ExistingKey_ReturnsTrueAndRuleIsGone()
    {
        var sut = new FailureInjectionService();
        sut.AddRule(new InjectionRule { Key = "remove-me", Type = InjectionType.Exception });

        var removed = sut.RemoveRule("remove-me");

        removed.Should().BeTrue();
        sut.GetRules().Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that <see cref="FailureInjectionService.RemoveRule(string)"/> returns false when removing a non-existent rule.
    /// </summary>
    [Fact]
    public void RemoveRule_NonExistentKey_ReturnsFalse()
    {
        var sut = new FailureInjectionService();

        var removed = sut.RemoveRule("does-not-exist");

        removed.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="FailureInjectionService.GetRules()"/> returns all registered rules.
    /// </summary>
    [Fact]
    public void GetRules_ReturnsAllRegisteredRules()
    {
        var sut = new FailureInjectionService();
        var rule1 = new InjectionRule { Key = "r1", Type = InjectionType.Exception };
        var rule2 = new InjectionRule { Key = "r2", Type = InjectionType.Latency };
        var rule3 = new InjectionRule { Key = "r3", Type = InjectionType.Timeout };

        sut.AddRule(rule1);
        sut.AddRule(rule2);
        sut.AddRule(rule3);

        var rules = sut.GetRules();

        rules.Should().HaveCount(3);
        rules.Should().Contain(r => r.Key == "r1");
        rules.Should().Contain(r => r.Key == "r2");
        rules.Should().Contain(r => r.Key == "r3");
    }

    /// <summary>
    /// Verifies that <see cref="FailureInjectionService.AddRule(InjectionRule)"/> replaces existing rules with the same key.
    /// </summary>
    [Fact]
    public void AddRule_ReplacesExistingRuleWithSameKey()
    {
        var sut = new FailureInjectionService();
        var rule1 = new InjectionRule { Key = "replace-me", Type = InjectionType.Exception, InjectionRate = 0.5 };
        var rule2 = new InjectionRule { Key = "replace-me", Type = InjectionType.Exception, InjectionRate = 1.0 };

        sut.AddRule(rule1);
        sut.AddRule(rule2);

        var rules = sut.GetRules();
        rules.Should().HaveCount(1);
        rules[0].InjectionRate.Should().Be(1.0);
    }

    /// <summary>
    /// Verifies that <see cref="FailureInjectionService.ExecuteAsync{T}(string, Func{CancellationToken, Task{T}}, CancellationToken)"/>
    /// throws <see cref="ArgumentNullException"/> when operation is null.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithNullOperation_ThrowsArgumentNullException()
    {
        var sut = new FailureInjectionService();

        Func<Task> act = () => sut.ExecuteAsync<string>("test", null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("operation");
    }

    /// <summary>
    /// Verifies that void operations work correctly with failure injection.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_VoidOperation_WorksCorrectly()
    {
        var sut = new FailureInjectionService();
        var executed = false;

        sut.AddRule(new InjectionRule
        {
            Key = "void-op",
            Type = InjectionType.Exception,
            InjectionRate = 1.0,
            ExceptionMessage = "void failure"
        });

        Func<Task> act = () => sut.ExecuteAsync("void-op", _ =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        await act.Should().ThrowAsync<InjectedFaultException>();
        executed.Should().BeFalse(); // Operation should not execute when exception is injected
    }

    /// <summary>
    /// Verifies that latency injection works with different delay values.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_LatencyRule_WithCustomDelay_AppliesCorrectDelay()
    {
        var sut = new FailureInjectionService();
        sut.AddRule(new InjectionRule
        {
            Key = "custom-latency",
            Type = InjectionType.Latency,
            InjectionRate = 1.0,
            LatencyDelay = TimeSpan.FromMilliseconds(200)
        });

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await sut.ExecuteAsync("custom-latency", _ => Task.FromResult("result"));
        sw.Stop();

        result.Should().Be("result");
        sw.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(180);
    }

    /// <summary>
    /// Verifies that timeout injection respects cancellation tokens.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_TimeoutRule_RespectsCancellationToken()
    {
        var sut = new FailureInjectionService();
        using var cts = new CancellationTokenSource();

        sut.AddRule(new InjectionRule
        {
            Key = "timeout-cancel",
            Type = InjectionType.Timeout,
            InjectionRate = 1.0,
            TimeoutDuration = TimeSpan.FromSeconds(10)
        });

        cts.CancelAfter(50);

        Func<Task> act = () => sut.ExecuteAsync("timeout-cancel", _ => Task.FromResult(42), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}