#nullable enable

using System.Text.RegularExpressions;
using DotNetResiliencePipeline.Utilities;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

/// <summary>
/// Extension methods for <see cref="PolicyNameGeneratorTests"/> that provide additional testing utilities
/// for the <see cref="PolicyNameGenerator"/> class.
/// </summary>
public static class PolicyNameGeneratorTestsExtensions
{
    /// <summary>
    /// Creates a new <see cref="PolicyNameGenerator"/> instance and verifies it starts with a clean state.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <returns>A new <see cref="PolicyNameGenerator"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="test"/> is null.</exception>
    public static PolicyNameGenerator CreateCleanGenerator(this PolicyNameGeneratorTests test)
    {
        ArgumentNullException.ThrowIfNull(test);

        var generator = new PolicyNameGenerator();

        // Verify clean state
        generator.GetAllRegisteredNames().Should().BeEmpty();

        return generator;
    }

    /// <summary>
    /// Generates multiple names from the same service and policy type and returns them as a collection.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="serviceName">The service name.</param>
    /// <param name="policyType">The policy type.</param>
    /// <param name="count">The number of names to generate.</param>
    /// <returns>A collection of generated names.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="test"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="count"/> is less than 1.</exception>
    public static IReadOnlyList<string> GenerateMultipleNames(
        this PolicyNameGeneratorTests test,
        string serviceName,
        string policyType,
        int count)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

        var generator = new PolicyNameGenerator();
        var names = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            names.Add(generator.GenerateName(serviceName, policyType));
        }

        return names.AsReadOnly();
    }

    /// <summary>
    /// Verifies that all generated names are unique within the same service and policy type.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="names">The collection of generated names.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="test"/> or <paramref name="names"/> is null.</exception>
    public static void AllNamesShouldBeUnique(
        this PolicyNameGeneratorTests test,
        IReadOnlyList<string> names)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(names);

        names.Should().OnlyHaveUniqueItems("All generated names should be unique");
    }

    /// <summary>
    /// Verifies that names follow the expected pattern for a given policy type.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="names">The collection of generated names.</param>
    /// <param name="expectedPrefix">The expected prefix before the counter (e.g., "payment-cb-").</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="test"/> or <paramref name="names"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="expectedPrefix"/> is null or empty.</exception>
    public static void NamesShouldMatchPattern(
        this PolicyNameGeneratorTests test,
        IReadOnlyList<string> names,
        string expectedPrefix)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(names);
        ArgumentException.ThrowIfNullOrEmpty(expectedPrefix);

        foreach (var name in names)
        {
            name.Should().StartWith(expectedPrefix, $"Name '{name}' should start with '{expectedPrefix}'");
            name.Should().MatchRegex(
                @$"^{Regex.Escape(expectedPrefix)}\d+$",
                $"Name '{name}' should match pattern '{expectedPrefix}<number>'");
        }
    }

    /// <summary>
    /// Generates a descriptive name and verifies it contains all expected parts in order.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="serviceName">The service name.</param>
    /// <param name="policyType">The policy type.</param>
    /// <param name="purpose">The purpose/scenario.</param>
    /// <param name="expectedParts">The expected parts in order.</param>
    /// <returns>The generated descriptive name.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="test"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="expectedParts"/> is null or empty.</exception>
    public static string GenerateAndVerifyDescriptiveName(
        this PolicyNameGeneratorTests test,
        string serviceName,
        string policyType,
        string purpose,
        string[] expectedParts)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentException.ThrowIfNullOrEmpty(serviceName);
        ArgumentException.ThrowIfNullOrEmpty(policyType);
        ArgumentException.ThrowIfNullOrEmpty(purpose);
        ArgumentNullException.ThrowIfNull(expectedParts);

        var generator = new PolicyNameGenerator();
        var name = generator.GenerateDescriptiveName(serviceName, policyType, purpose);

        var parts = name.Split('-');
        parts.Should().HaveCount(expectedParts.Length, $"Name should have {expectedParts.Length} parts");

        for (var i = 0; i < expectedParts.Length; i++)
        {
            parts[i].Should().Be(expectedParts[i], $"Part {i} should be '{expectedParts[i]}'");
        }

        return name;
    }

    /// <summary>
    /// Verifies that a name is valid according to the policy name rules.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="name">The policy name to validate.</param>
    /// <param name="expectedIsValid">Whether the name is expected to be valid.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="test"/> is null.</exception>
    public static void NameShouldBeValid(
        this PolicyNameGeneratorTests test,
        string name,
        bool expectedIsValid)
    {
        ArgumentNullException.ThrowIfNull(test);

        var generator = new PolicyNameGenerator();
        var isValid = generator.IsValidPolicyName(name);

        if (expectedIsValid)
        {
            isValid.Should().BeTrue($"Name '{name}' should be valid");
        }
        else
        {
            isValid.Should().BeFalse($"Name '{name}' should be invalid");
        }
    }

    /// <summary>
    /// Registers multiple names and verifies they are all tracked.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="names">The names to register.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="test"/> or <paramref name="names"/> is null.</exception>
    public static void RegisterMultipleNames(
        this PolicyNameGeneratorTests test,
        IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(names);

        var generator = new PolicyNameGenerator();

        foreach (var name in names)
        {
            generator.RegisterName(name);
        }

        var registeredNames = generator.GetAllRegisteredNames();
        foreach (var name in names)
        {
            registeredNames.Should().Contain(name);
        }
    }

    /// <summary>
    /// Generates a name with a prefix and verifies the prefix is applied correctly.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="prefix">The prefix to apply.</param>
    /// <param name="serviceName">The service name.</param>
    /// <param name="policyType">The policy type.</param>
    /// <returns>The generated name with prefix.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="test"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="prefix"/>, <paramref name="serviceName"/>, or <paramref name="policyType"/> is null or empty.</exception>
    public static string GenerateNameWithPrefixAndVerify(
        this PolicyNameGeneratorTests test,
        string prefix,
        string serviceName,
        string policyType)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentException.ThrowIfNullOrEmpty(prefix);
        ArgumentException.ThrowIfNullOrEmpty(serviceName);
        ArgumentException.ThrowIfNullOrEmpty(policyType);

        var generator = new PolicyNameGenerator();
        var name = generator.GenerateNameWithPrefix(prefix, serviceName, policyType);

        name.Should().StartWith($"{prefix}-{serviceName}-{policyType}-");

        return name;
    }
}