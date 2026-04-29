#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Text;

namespace DotNetResiliencePipeline.Utilities;

/// <summary>
/// Generates meaningful, unique, and consistent policy names.
/// Supports naming conventions, auto-numbering, and namespace prefixing.
/// </summary>
public sealed class PolicyNameGenerator
{
    private readonly ConcurrentDictionary<string, int> _nameCounters = new();
    private readonly HashSet<string> _usedNames = new();
    private readonly object _lockObj = new object();

    /// <summary>
    /// Generates a policy name based on service and policy type.
    /// </summary>
    public string GenerateName(string serviceName, string policyType, int? customNumber = null)
    {
        var baseName = NormalizeServiceName(serviceName);
        var typeSuffix = policyType.ToLowerInvariant() switch
        {
            "circuitbreaker" => "cb",
            "retry" => "retry",
            "timeout" => "timeout",
            "bulkhead" => "bulkhead",
            "fallback" => "fallback",
            _ => policyType.Substring(0, Math.Min(3, policyType.Length)).ToLowerInvariant()
        };

        lock (_lockObj)
        {
            int number = customNumber ?? IncrementCounter($"{baseName}_{typeSuffix}");
            string name = $"{baseName}-{typeSuffix}-{number}";

            // Ensure uniqueness
            while (_usedNames.Contains(name))
            {
                number++;
                name = $"{baseName}-{typeSuffix}-{number}";
            }

            _usedNames.Add(name);
            return name;
        }
    }

    /// <summary>
    /// Generates a name with a prefix for organizational purposes.
    /// </summary>
    public string GenerateNameWithPrefix(string prefix, string serviceName, string policyType)
    {
        var baseName = GenerateName(serviceName, policyType);
        return $"{prefix}-{baseName}";
    }

    /// <summary>
    /// Generates a descriptive policy name.
    /// </summary>
    public string GenerateDescriptiveName(string serviceName, string policyType, string? purpose = null)
    {
        var parts = new List<string>();

        parts.Add(NormalizeServiceName(serviceName));

        if (!string.IsNullOrEmpty(purpose))
            parts.Add(NormalizePurpose(purpose));

        parts.Add(policyType.ToLowerInvariant());

        return string.Join("-", parts);
    }

    /// <summary>
    /// Validates if a policy name is properly formatted.
    /// </summary>
    public bool IsValidPolicyName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (name.Length < 3 || name.Length > 100)
            return false;

        // Allow alphanumeric, dash, underscore
        return System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z0-9_-]+$");
    }

    /// <summary>
    /// Suggests a policy name based on context.
    /// </summary>
    public string SuggestName(string serviceName, string operation, string failureScenario)
    {
        var service = NormalizeServiceName(serviceName);
        var op = NormalizePurpose(operation);
        var scenario = NormalizePurpose(failureScenario);

        return $"{service}-{op}-{scenario}";
    }

    /// <summary>
    /// Registers an existing policy name as used.
    /// </summary>
    public void RegisterName(string name)
    {
        lock (_lockObj)
        {
            _usedNames.Add(name);
        }
    }

    /// <summary>
    /// Releases a policy name so it can be reused.
    /// </summary>
    public void UnregisterName(string name)
    {
        lock (_lockObj)
        {
            _usedNames.Remove(name);
        }
    }

    /// <summary>
    /// Gets all registered names.
    /// </summary>
    public List<string> GetAllRegisteredNames()
    {
        lock (_lockObj)
        {
            return new List<string>(_usedNames);
        }
    }

    /// <summary>
    /// Clears all registrations.
    /// </summary>
    public void Clear()
    {
        lock (_lockObj)
        {
            _usedNames.Clear();
            _nameCounters.Clear();
        }
    }

    private string NormalizeServiceName(string name)
    {
        var normalized = System.Text.RegularExpressions.Regex.Replace(
            name.ToLowerInvariant(),
            @"[^a-z0-9]",
            "-");

        return normalized.Trim('-');
    }

    private string NormalizePurpose(string purpose)
    {
        var normalized = System.Text.RegularExpressions.Regex.Replace(
            purpose.ToLowerInvariant(),
            @"[^a-z0-9]",
            "-");

        return normalized.Trim('-');
    }

    private int IncrementCounter(string key)
    {
        return _nameCounters.AddOrUpdate(key, 1, (k, v) => v + 1);
    }
}

/// <summary>
/// Suggested naming convention template for policies.
/// </summary>
public sealed class NamingTemplate
{
    public string Service { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string PolicyType { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Builds a policy name from template values.
    /// </summary>
    public string BuildName()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(Service))
            parts.Add(Service.ToLowerInvariant());

        if (!string.IsNullOrEmpty(Operation))
            parts.Add(Operation.ToLowerInvariant());

        if (!string.IsNullOrEmpty(PolicyType))
            parts.Add(PolicyType.ToLowerInvariant());

        if (!string.IsNullOrEmpty(Environment))
            parts.Add(Environment.ToLowerInvariant());

        return string.Join("-", parts);
    }
}
