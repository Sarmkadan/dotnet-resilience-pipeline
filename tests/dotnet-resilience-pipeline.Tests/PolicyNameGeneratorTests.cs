#nullable enable
using DotNetResiliencePipeline.Utilities;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public sealed class PolicyNameGeneratorTests
{
    [Fact]
    public void GenerateName_WithKnownPolicyType_UsesCorrectSuffix()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.GenerateName("payment", "circuitbreaker");

        name.Should().StartWith("payment-cb-");
    }

    [Fact]
    public void GenerateName_RetryType_UsesRetrySuffix()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.GenerateName("order", "retry");

        name.Should().StartWith("order-retry-");
    }

    [Fact]
    public void GenerateName_TimeoutType_UsesTimeoutSuffix()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.GenerateName("catalog", "timeout");

        name.Should().StartWith("catalog-timeout-");
    }

    [Fact]
    public void GenerateName_BulkheadType_UsesBulkheadSuffix()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.GenerateName("inventory", "bulkhead");

        name.Should().StartWith("inventory-bulkhead-");
    }

    [Fact]
    public void GenerateName_FallbackType_UsesFallbackSuffix()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.GenerateName("shipping", "fallback");

        name.Should().StartWith("shipping-fallback-");
    }

    [Fact]
    public void GenerateName_SameServiceAndType_ProducesUniqueNames()
    {
        var generator = new PolicyNameGenerator();

        var name1 = generator.GenerateName("api", "retry");
        var name2 = generator.GenerateName("api", "retry");

        name1.Should().NotBe(name2);
    }

    [Fact]
    public void GenerateName_ServiceNameWithSpecialChars_NormalizesToDashes()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.GenerateName("Payment Service", "retry");

        name.Should().MatchRegex(@"^[a-z0-9_-]+-retry-\d+$");
        name.Should().NotContain(" ");
    }

    [Fact]
    public void GenerateName_CustomNumber_UsesProvidedNumber()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.GenerateName("svc", "timeout", customNumber: 42);

        name.Should().Be("svc-timeout-42");
    }

    [Fact]
    public void GenerateDescriptiveName_WithPurpose_IncludesAllParts()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.GenerateDescriptiveName("payment", "retry", "network");

        name.Should().Be("payment-network-retry");
    }

    [Fact]
    public void GenerateDescriptiveName_WithoutPurpose_SkipsPurposePart()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.GenerateDescriptiveName("payment", "timeout");

        name.Should().Be("payment-timeout");
    }

    [Fact]
    public void IsValidPolicyName_WithValidName_ReturnsTrue()
    {
        var generator = new PolicyNameGenerator();

        generator.IsValidPolicyName("payment-cb-1").Should().BeTrue();
        generator.IsValidPolicyName("order_retry").Should().BeTrue();
        generator.IsValidPolicyName("abc").Should().BeTrue();
    }

    [Fact]
    public void IsValidPolicyName_WithNullOrWhitespace_ReturnsFalse()
    {
        var generator = new PolicyNameGenerator();

        generator.IsValidPolicyName(null!).Should().BeFalse();
        generator.IsValidPolicyName("").Should().BeFalse();
        generator.IsValidPolicyName("  ").Should().BeFalse();
    }

    [Fact]
    public void IsValidPolicyName_TooShort_ReturnsFalse()
    {
        var generator = new PolicyNameGenerator();

        generator.IsValidPolicyName("ab").Should().BeFalse();
    }

    [Fact]
    public void IsValidPolicyName_TooLong_ReturnsFalse()
    {
        var generator = new PolicyNameGenerator();
        var longName = new string('a', 101);

        generator.IsValidPolicyName(longName).Should().BeFalse();
    }

    [Fact]
    public void IsValidPolicyName_WithSpecialChars_ReturnsFalse()
    {
        var generator = new PolicyNameGenerator();

        generator.IsValidPolicyName("my name!").Should().BeFalse();
        generator.IsValidPolicyName("name@service").Should().BeFalse();
    }

    [Fact]
    public void SuggestName_CombinesServiceOperationAndScenario()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.SuggestName("payment", "charge", "network-error");

        name.Should().Be("payment-charge-network-error");
    }

    [Fact]
    public void RegisterName_PreventsDuplicateGeneration()
    {
        var generator = new PolicyNameGenerator();
        generator.RegisterName("svc-retry-1");

        var name = generator.GenerateName("svc", "retry", customNumber: 1);

        name.Should().NotBe("svc-retry-1");
    }

    [Fact]
    public void UnregisterName_AllowsNameToBeUsedAgain()
    {
        var generator = new PolicyNameGenerator();
        generator.RegisterName("svc-cb-1");
        generator.UnregisterName("svc-cb-1");

        var allNames = generator.GetAllRegisteredNames();

        allNames.Should().NotContain("svc-cb-1");
    }

    [Fact]
    public void GetAllRegisteredNames_ReturnsAllRegistered()
    {
        var generator = new PolicyNameGenerator();
        generator.RegisterName("alpha-cb-1");
        generator.RegisterName("beta-retry-2");

        var names = generator.GetAllRegisteredNames();

        names.Should().Contain("alpha-cb-1");
        names.Should().Contain("beta-retry-2");
    }

    [Fact]
    public void Clear_RemovesAllRegistrationsAndCounters()
    {
        var generator = new PolicyNameGenerator();
        generator.GenerateName("svc", "retry");
        generator.Clear();

        var names = generator.GetAllRegisteredNames();
        names.Should().BeEmpty();

        var nameAfterClear = generator.GenerateName("svc", "retry");
        nameAfterClear.Should().EndWith("-1");
    }

    [Fact]
    public void GenerateNameWithPrefix_PrependsPrefixToBaseName()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.GenerateNameWithPrefix("prod", "api", "timeout");

        name.Should().StartWith("prod-api-timeout-");
    }
}

public sealed class NamingTemplateTests
{
    [Fact]
    public void BuildName_AllFields_JoinsWithDash()
    {
        var template = new NamingTemplate
        {
            Service = "Payment",
            Operation = "Charge",
            PolicyType = "Retry",
            Environment = "Prod"
        };

        var name = template.BuildName();

        name.Should().Be("payment-charge-retry-prod");
    }

    [Fact]
    public void BuildName_EmptyOptionalFields_OmitsEmptyParts()
    {
        var template = new NamingTemplate
        {
            Service = "Catalog",
            PolicyType = "Timeout"
        };

        var name = template.BuildName();

        name.Should().Be("catalog-timeout");
    }

    [Fact]
    public void BuildName_EmptyTemplate_ReturnsEmptyString()
    {
        var template = new NamingTemplate();

        var name = template.BuildName();

        name.Should().BeEmpty();
    }
}
