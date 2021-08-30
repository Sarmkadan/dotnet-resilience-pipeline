#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Services;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public sealed class FailureInjectionServiceTests
{
    [Fact]
    public async Task ExecuteAsync_NoRuleRegistered_RunsOperationNormally()
    {
        var sut = new FailureInjectionService();

        var result = await sut.ExecuteAsync("missing", _ => Task.FromResult(42));

        result.Should().Be(42);
    }

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

    [Fact]
    public void DisableAll_DeactivatesAllRules()
    {
        var sut = new FailureInjectionService();
        sut.AddRule(new InjectionRule { Key = "r1", Type = InjectionType.Exception, InjectionRate = 1.0 });
        sut.AddRule(new InjectionRule { Key = "r2", Type = InjectionType.Exception, InjectionRate = 1.0 });

        sut.DisableAll();

        sut.GetRules().Should().AllSatisfy(r => r.IsEnabled.Should().BeFalse());
    }

    [Fact]
    public void RemoveRule_ExistingKey_ReturnsTrueAndRuleIsGone()
    {
        var sut = new FailureInjectionService();
        sut.AddRule(new InjectionRule { Key = "remove-me", Type = InjectionType.Exception });

        var removed = sut.RemoveRule("remove-me");

        removed.Should().BeTrue();
        sut.GetRules().Should().BeEmpty();
    }
}
