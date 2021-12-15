#nullable enable
using DotNetResiliencePipeline.Utilities;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

/// <summary>
/// Contains unit tests for the <see cref="PolicyNameGenerator"/> class.
/// Tests various naming conventions and validations for resilience policy names.
/// </summary>
public sealed class PolicyNameGeneratorTests
{
    /// <summary>
    /// Tests that <see cref="PolicyNameGenerator.GenerateName(string, string)"/> generates names with correct suffix for circuit breaker policies.
    /// </summary>
    [Fact]
    public void GenerateName_WithKnownPolicyType_UsesCorrectSuffix()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.GenerateName("payment", "circuitbreaker");

        name.Should().StartWith("payment-cb-");
    }

    /// <summary>
    /// Tests that <see cref="PolicyNameGenerator.GenerateName(string, string)"/> generates names with correct suffix for retry policies.
    /// </summary>
    [Fact]
    public void GenerateName_RetryType_UsesRetrySuffix()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.GenerateName("order", "retry");

        name.Should().StartWith("order-retry-");
    }

    /// <summary>
    /// Tests that <see cref="PolicyNameGenerator.GenerateName(string, string)"/> generates names with correct suffix for timeout policies.
    /// </summary>
    [Fact]
    public void GenerateName_TimeoutType_UsesTimeoutSuffix()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.GenerateName("catalog", "timeout");

        name.Should().StartWith("catalog-timeout-");
    }

    /// <summary>
    /// Tests that <see cref="PolicyNameGenerator.GenerateName(string, string)"/> generates names with correct suffix for bulkhead policies.
    /// </summary>
    [Fact]
    public void GenerateName_BulkheadType_UsesBulkheadSuffix()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.GenerateName("inventory", "bulkhead");

        name.Should().StartWith("inventory-bulkhead-");
    }

    /// <summary>
    /// Tests that <see cref="PolicyNameGenerator.GenerateName(string, string)"/> generates names with correct suffix for fallback policies.
    /// </summary>
    [Fact]
    public void GenerateName_FallbackType_UsesFallbackSuffix()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.GenerateName("shipping", "fallback");

        name.Should().StartWith("shipping-fallback-");
    }

    /// <summary>
    /// Tests that <see cref="PolicyNameGenerator.GenerateName(string, string)"/> generates unique names for the same service and policy type.
    /// Verifies that the method appends incrementing numbers to ensure uniqueness.
    /// </summary>
    [Fact]
    public void GenerateName_SameServiceAndType_ProducesUniqueNames()
    {
        var generator = new PolicyNameGenerator();

        var name1 = generator.GenerateName("api", "retry");
        var name2 = generator.GenerateName("api", "retry");

        name1.Should().NotBe(name2);
    }

    /// <summary>
    /// Tests that <see cref="PolicyNameGenerator.GenerateName(string, string)"/> normalizes service names with special characters to use dashes.
    /// Verifies that spaces are replaced with dashes and the result matches the expected naming pattern.
    /// </summary>
    [Fact]
    public void GenerateName_ServiceNameWithSpecialChars_NormalizesToDashes()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.GenerateName("Payment Service", "retry");

        name.Should().MatchRegex(@"^[a-z0-9_-]+-retry-\d+$");
        name.Should().NotContain(" ");
    }

    /// <summary>
    /// Tests that <see cref="PolicyNameGenerator.GenerateName(string, string, int?)"/> uses the provided custom number when generating policy names.
    /// Verifies that the custom number parameter overrides the auto-incremented counter.
    /// </summary>
    [Fact]
    public void GenerateName_CustomNumber_UsesProvidedNumber()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.GenerateName("svc", "timeout", customNumber: 42);

        name.Should().Be("svc-timeout-42");
    }

    /// <summary>
    /// Tests that <see cref="PolicyNameGenerator.GenerateDescriptiveName(string, string, string)"/> includes all parts when a purpose is provided.
    /// Verifies that the generated name combines service, purpose, and policy type in the correct order.
    /// </summary>
    [Fact]
    public void GenerateDescriptiveName_WithPurpose_IncludesAllParts()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.GenerateDescriptiveName("payment", "retry", "network");

        name.Should().Be("payment-network-retry");
    }

    /// <summary>
    /// Tests that <see cref="PolicyNameGenerator.GenerateDescriptiveName(string, string, string)"/> omits the purpose part when null or empty.
    /// Verifies that the generated name only includes service and policy type when no purpose is provided.
    /// </summary>
    [Fact]
    public void GenerateDescriptiveName_WithoutPurpose_SkipsPurposePart()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.GenerateDescriptiveName("payment", "timeout");

        name.Should().Be("payment-timeout");
    }

    /// <summary>
    /// Tests that <see cref="PolicyNameGenerator.IsValidPolicyName(string)"/> returns true for valid policy names.
    /// Valid names include kebab-case with numbers, underscores, and simple lowercase names.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="PolicyNameGenerator.IsValidPolicyName(string)"/> returns false for names that are too short.
    /// Verifies that the minimum length requirement of 3 characters is enforced.
    /// </summary>
    [Fact]
    public void IsValidPolicyName_TooShort_ReturnsFalse()
    {
        var generator = new PolicyNameGenerator();

        generator.IsValidPolicyName("ab").Should().BeFalse();
    }

    /// <summary>
    /// Tests that <see cref="PolicyNameGenerator.IsValidPolicyName(string)"/> returns false for names that exceed the maximum length.
    /// Verifies that the maximum length requirement of 100 characters is enforced.
    /// </summary>
    [Fact]
    public void IsValidPolicyName_TooLong_ReturnsFalse()
    {
        var generator = new PolicyNameGenerator();
        var longName = new string('a', 101);

        generator.IsValidPolicyName(longName).Should().BeFalse();
    }

    /// <summary>
    /// Tests that <see cref="PolicyNameGenerator.IsValidPolicyName(string)"/> returns false for names containing invalid special characters.
    /// Verifies that only alphanumeric characters, dashes, underscores, and dots are allowed.
    /// </summary>
    [Fact]
    public void IsValidPolicyName_WithSpecialChars_ReturnsFalse()
    {
        var generator = new PolicyNameGenerator();

        generator.IsValidPolicyName("my name!").Should().BeFalse();
        generator.IsValidPolicyName("name@service").Should().BeFalse();
    }

    /// <summary>
    /// Tests that <see cref="PolicyNameGenerator.SuggestName(string, string, string)"/> combines service, operation, and scenario into a single policy name.
    /// Verifies that the generated name follows the pattern "service-operation-scenario".
    /// </summary>
    [Fact]
    public void SuggestName_CombinesServiceOperationAndScenario()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.SuggestName("payment", "charge", "network-error");

        name.Should().Be("payment-charge-network-error");
    }

    /// <summary>
    /// Tests that <see cref="PolicyNameGenerator.RegisterName(string)"/> prevents duplicate name generation.
    /// Verifies that registered names are excluded from future generation attempts.
    /// </summary>
    [Fact]
    public void RegisterName_PreventsDuplicateGeneration()
    {
        var generator = new PolicyNameGenerator();
        generator.RegisterName("svc-retry-1");

        var name = generator.GenerateName("svc", "retry", customNumber: 1);

        name.Should().NotBe("svc-retry-1");
    }

    /// <summary>
    /// Tests that <see cref="PolicyNameGenerator.UnregisterName(string)"/> allows previously registered names to be used again.
    /// Verifies that unregistered names are removed from the tracking set and can be regenerated.
    /// </summary>
    [Fact]
    public void UnregisterName_AllowsNameToBeUsedAgain()
    {
        var generator = new PolicyNameGenerator();
        generator.RegisterName("svc-cb-1");
        generator.UnregisterName("svc-cb-1");

        var allNames = generator.GetAllRegisteredNames();

        allNames.Should().NotContain("svc-cb-1");
    }

    /// <summary>
    /// Tests that <see cref="PolicyNameGenerator.GetAllRegisteredNames()"/> returns all currently registered policy names.
    /// Verifies that the method returns a collection containing all previously registered names.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="PolicyNameGenerator.Clear()"/> removes all registered names and resets counters.
    /// Verifies that after clearing, new names start from counter 1 again.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="PolicyNameGenerator.GenerateNameWithPrefix(string, string, string)"/> prepends the environment prefix to the base policy name.
    /// Verifies that the generated name follows the pattern "environment-service-policyType-".
    /// </summary>
    [Fact]
    public void GenerateNameWithPrefix_PrependsPrefixToBaseName()
    {
        var generator = new PolicyNameGenerator();

        var name = generator.GenerateNameWithPrefix("prod", "api", "timeout");

        name.Should().StartWith("prod-api-timeout-");
    }
}

/// <summary>
/// Contains unit tests for the <see cref="NamingTemplate"/> class.
/// Tests the template-based naming functionality for building policy names from components.
/// </summary>
public sealed class NamingTemplateTests
{
    /// <summary>
    /// Tests that <see cref="NamingTemplate.BuildName()"/> joins all template fields with dashes when all fields are populated.
    /// Verifies that the service, operation, policy type, and environment are combined in the correct order.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="NamingTemplate.BuildName()"/> omits empty optional fields from the generated name.
    /// Verifies that only populated fields (Service and PolicyType) are included in the result.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="NamingTemplate.BuildName()"/> returns an empty string when the template is completely empty.
    /// Verifies that the method handles completely uninitialized templates gracefully.
    /// </summary>
    [Fact]
    public void BuildName_EmptyTemplate_ReturnsEmptyString()
    {
        var template = new NamingTemplate();

        var name = template.BuildName();

        name.Should().BeEmpty();
    }
}
