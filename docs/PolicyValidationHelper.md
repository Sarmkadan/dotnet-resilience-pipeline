# PolicyValidationHelper

The `PolicyValidationHelper` class provides a comprehensive suite of static and instance members designed to analyze, validate, and optimize resilience policies within the `dotnet-resilience-pipeline` framework. It serves as a diagnostic tool that inspects policy configurations for structural integrity, identifies common anti-patterns that may lead to runtime failures or performance degradation, and offers actionable suggestions for improvement. The helper aggregates findings into a structured report containing errors, warnings, and optimization tips, facilitating robust policy design before deployment.

## API

### `public static ValidationReport ValidatePolicy`
Analyzes a specified resilience policy configuration to ensure it adheres to framework constraints and logical consistency rules.
*   **Purpose**: Performs a deep validation check on a policy definition.
*   **Parameters**: Accepts the policy object or configuration required for validation (specific parameter types depend on the overloaded implementation context).
*   **Return Value**: Returns a `ValidationReport` instance containing the aggregate results of the validation process, including status, errors, and warnings.
*   **Throws**: Throws an `ArgumentNullException` if the input policy is null; may throw `ArgumentException` if the policy structure is fundamentally malformed.

### `public static List<string> IdentifyAntiPatterns`
Scans a policy configuration to detect known anti-patterns that compromise resilience or stability.
*   **Purpose**: Identifies specific design flaws such as infinite retry loops, missing cancellation tokens, or conflicting timeout settings.
*   **Parameters**: Accepts the policy configuration to be analyzed.
*   **Return Value**: Returns a `List<string>` where each element describes a detected anti-pattern. Returns an empty list if no anti-patterns are found.
*   **Throws**: Throws `ArgumentNullException` if the input is null.

### `public static List<string> SuggestOptimizations`
Evaluates a policy to recommend performance improvements or best practice alignments.
*   **Purpose**: Provides non-critical suggestions to enhance efficiency, such as adjusting retry intervals or consolidating redundant handlers.
*   **Parameters**: Accepts the policy configuration to be evaluated.
*   **Return Value**: Returns a `List<string>` containing descriptive optimization suggestions. Returns an empty list if the policy is already optimal.
*   **Throws**: Throws `ArgumentNullException` if the input is null.

### `public string PolicyId`
Gets the unique identifier associated with the specific policy instance currently being analyzed or reported on.
*   **Purpose**: Uniquely identifies the policy within the validation context.
*   **Return Value**: A string representing the policy ID.
*   **Throws**: Does not throw.

### `public string PolicyName`
Gets the human-readable name assigned to the policy instance.
*   **Purpose**: Provides a descriptive label for the policy for logging and reporting purposes.
*   **Return Value**: A string representing the policy name.
*   **Throws**: Does not throw.

### `public List<string> Errors`
Gets the collection of critical validation errors encountered during the analysis of the policy.
*   **Purpose**: Lists issues that prevent the policy from being considered valid or safe for execution.
*   **Return Value**: A `List<string>` containing error messages. The list is never null but may be empty.
*   **Throws**: Does not throw.

### `public List<string> Warnings`
Gets the collection of non-critical issues detected during the analysis.
*   **Purpose**: Lists potential risks or suboptimal configurations that do not strictly invalidate the policy but warrant attention.
*   **Return Value**: A `List<string>` containing warning messages. The list is never null but may be empty.
*   **Throws**: Does not throw.

### `public List<string> Suggestions`
Gets the collection of optimization recommendations generated for the policy.
*   **Purpose**: Provides advisory feedback on how to improve the policy's performance or maintainability.
*   **Return Value**: A `List<string>` containing suggestion messages. The list is never null but may be empty.
*   **Throws**: Does not throw.

### `public override string ToString`
Returns a string representation of the current `PolicyValidationHelper` instance, typically summarizing the validation state.
*   **Purpose**: Provides a quick textual summary of the policy ID, name, and count of errors/warnings for debugging or logging.
*   **Parameters**: None.
*   **Return Value**: A formatted string summarizing the instance state.
*   **Throws**: Does not throw.

## Usage

### Example 1: Validating a Policy and Handling Errors
This example demonstrates how to validate a constructed policy and iterate through critical errors if validation fails.

```csharp
using DotNet.Resilience.Pipeline;
using System;
using System.Linq;

public class PolicyValidator
{
    public void RunValidation()
    {
        // Assume 'myPolicy' is a constructed resilience policy object
        var policy = MyPolicyFactory.CreateRetryPolicy();

        // Perform static validation
        var report = PolicyValidationHelper.ValidatePolicy(policy);

        if (report.Errors.Any())
        {
            Console.WriteLine($"Validation failed for policy: {report.PolicyName}");
            foreach (var error in report.Errors)
            {
                Console.WriteLine($"[ERROR] {error}");
            }
            return;
        }

        Console.WriteLine($"Policy '{report.PolicyName}' is valid.");
    }
}
```

### Example 2: Analyzing Anti-Patterns and Optimizations
This example shows how to use the helper to identify design flaws and retrieve optimization suggestions independently of the strict validation flow.

```csharp
using DotNet.Resilience.Pipeline;
using System;
using System.Collections.Generic;

public class PolicyOptimizer
{
    public void AnalyzePolicy()
    {
        var policy = MyPolicyFactory.CreateComplexPipeline();

        // Identify anti-patterns
        List<string> antiPatterns = PolicyValidationHelper.IdentifyAntiPatterns(policy);
        if (antiPatterns.Count > 0)
        {
            Console.WriteLine("Detected Anti-Patterns:");
            antiPatterns.ForEach(p => Console.WriteLine($"- {p}"));
        }

        // Get optimization suggestions
        List<string> optimizations = PolicyValidationHelper.SuggestOptimizations(policy);
        if (optimizations.Count > 0)
        {
            Console.WriteLine("\nOptimization Suggestions:");
            optimizations.ForEach(s => Console.WriteLine($"* {s}"));
        }
        
        if (antiPatterns.Count == 0 && optimizations.Count == 0)
        {
            Console.WriteLine("Policy analysis complete: No issues or optimizations found.");
        }
    }
}
```

## Notes

*   **Thread Safety**: The static methods (`ValidatePolicy`, `IdentifyAntiPatterns`, `SuggestOptimizations`) are thread-safe and do not maintain internal mutable state between calls. They operate solely on the input provided. Instance properties (`Errors`, `Warnings`, `Suggestions`, `PolicyId`, `PolicyName`) reflect the state of the last operation performed on that specific instance context; if the helper is instantiated per-validation, no synchronization is required. If a single instance is shared across threads while being populated, external synchronization is necessary.
*   **Null Handling**: All static analysis methods strictly enforce non-null inputs. Passing a `null` policy configuration will result in an immediate `ArgumentNullException`. Callers should ensure policies are instantiated before passing them to the helper.
*   **Collection Mutability**: The lists returned by `Errors`, `Warnings`, `Suggestions`, and the static analysis methods are mutable. However, modifying these collections after retrieval does not affect the internal state of the validation engine or the source policy. They are snapshots of the analysis result at the time of generation.
*   **Edge Cases**: If a policy contains circular references or deeply nested structures that exceed the framework's recursion limits, `ValidatePolicy` may terminate early and populate the `Errors` list with a stack-overflow protection message rather than throwing a runtime exception. Empty policy configurations are treated as invalid and will generate specific errors regarding missing handlers.
