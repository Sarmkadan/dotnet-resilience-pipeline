# PolicyNameGeneratorTests

`PolicyNameGeneratorTests` is the test suite for the `PolicyNameGenerator` component in the `dotnet-resilience-pipeline` project. It validates the naming conventions, uniqueness guarantees, normalization rules, and lifecycle management (registration, unregistration, clearing) of resilience policy names. The tests ensure that generated names conform to expected formats, handle edge cases such as special characters and length constraints, and maintain correct internal state across multiple operations.

## API

### GenerateName_WithKnownPolicyType_UsesCorrectSuffix
Verifies that generating a name for a known policy type appends the expected suffix to the service name. The test supplies a service name and a policy type and asserts that the resulting name ends with the canonical suffix for that type.

### GenerateName_RetryType_UsesRetrySuffix
Specifically confirms that when the policy type is Retry, the generated name includes the Retry suffix. No parameters beyond the implicit service name and policy type; the assertion checks the suffix substring.

### GenerateName_TimeoutType_UsesTimeoutSuffix
Specifically confirms that when the policy type is Timeout, the generated name includes the Timeout suffix.

### GenerateName_BulkheadType_UsesBulkheadSuffix
Specifically confirms that when the policy type is Bulkhead, the generated name includes the Bulkhead suffix.

### GenerateName_FallbackType_UsesFallbackSuffix
Specifically confirms that when the policy type is Fallback, the generated name includes the Fallback suffix.

### GenerateName_SameServiceAndType_ProducesUniqueNames
Ensures that calling `GenerateName` multiple times with the same service name and policy type does not produce duplicate names. The test captures two or more generated names and asserts they are distinct, typically by verifying an incrementing counter or appended discriminator.

### GenerateName_ServiceNameWithSpecialChars_NormalizesToDashes
Validates that service names containing special characters (e.g., spaces, underscores, punctuation) are normalized by replacing non-alphanumeric sequences with dashes. The test passes a deliberately messy service name and checks that the output contains only alphanumeric characters and dashes.

### GenerateName_CustomNumber_UsesProvidedNumber
Tests the overload that accepts an explicit numeric suffix. The test supplies a custom number and asserts that the generated name incorporates that exact number rather than an auto-incremented value.

### GenerateDescriptiveName_WithPurpose_IncludesAllParts
Verifies that `GenerateDescriptiveName` produces a name containing the service, operation, scenario, and purpose when all four components are provided. The test checks that the resulting string contains each input part in the expected order and format.

### GenerateDescriptiveName_WithoutPurpose_SkipsPurposePart
Verifies that when the purpose argument is null, empty, or omitted, the descriptive name omits the purpose segment entirely while still including service, operation, and scenario.

### IsValidPolicyName_WithValidName_ReturnsTrue
Feeds a correctly formatted policy name to `IsValidPolicyName` and asserts that the method returns `true`. The name satisfies all length, character, and format constraints.

### IsValidPolicyName_WithNullOrWhitespace_ReturnsFalse
Passes null, empty, or whitespace-only strings to `IsValidPolicyName` and asserts that the method returns `false` for each case.

### IsValidPolicyName_TooShort_ReturnsFalse
Supplies a string shorter than the minimum allowed length and asserts that `IsValidPolicyName` returns `false`.

### IsValidPolicyName_TooLong_ReturnsFalse
Supplies a string exceeding the maximum allowed length and asserts that `IsValidPolicyName` returns `false`.

### IsValidPolicyName_WithSpecialChars_ReturnsFalse
Passes strings containing characters outside the permitted set (e.g., `@`, `#`, spaces) and asserts that `IsValidPolicyName` returns `false`.

### SuggestName_CombinesServiceOperationAndScenario
Tests the `SuggestName` method, confirming that it concatenates service, operation, and scenario into a single suggested name string following the prescribed template.

### RegisterName_PreventsDuplicateGeneration
Demonstrates that after a name is explicitly registered via `RegisterName`, subsequent calls to `GenerateName` with the same inputs will not produce the registered name. The test registers a name, then generates a name with matching parameters and asserts they differ.

### UnregisterName_AllowsNameToBeUsedAgain
Shows that calling `UnregisterName` on a previously registered name releases it, allowing `GenerateName` to produce that name again if the same inputs are supplied. The test registers, unregisters, and then generates, asserting the generated name matches the originally registered one.

### GetAllRegisteredNames_ReturnsAllRegistered
Registers multiple names and then calls `GetAllRegisteredNames`, asserting that the returned collection contains exactly those names and no others.

### Clear_RemovesAllRegistrationsAndCounters
Invokes `Clear` after performing registrations and name generations, then verifies that all internal counters reset and the registered name set is empty. Subsequent generations behave as if the generator is in a fresh state.

## Usage

```csharp
// Generate unique names for Retry policies on the same service
var generator = new PolicyNameGenerator();

string firstName = generator.GenerateName("PaymentService", PolicyType.Retry);
string secondName = generator.GenerateName("PaymentService", PolicyType.Retry);

// firstName and secondName are distinct, e.g. "PaymentService-Retry" and "PaymentService-Retry-2"
Console.WriteLine(firstName);
Console.WriteLine(secondName);

// Validate and register a custom name
if (PolicyNameGenerator.IsValidPolicyName("OrderIngestion-Bulkhead-Primary"))
{
    generator.RegisterName("OrderIngestion-Bulkhead-Primary");
}

// Later, unregister it when the policy is removed
generator.UnregisterName("OrderIngestion-Bulkhead-Primary");
```

```csharp
// Build a descriptive name with all components
var generator = new PolicyNameGenerator();

string descriptive = generator.GenerateDescriptiveName(
    service: "InventoryApi",
    operation: "CheckStock",
    scenario: "HighLoad",
    purpose: "Throttling"
);
// Result: "InventoryApi-CheckStock-HighLoad-Throttling"

// Omit purpose for a shorter name
string shortDescriptive = generator.GenerateDescriptiveName(
    service: "InventoryApi",
    operation: "CheckStock",
    scenario: "HighLoad"
);
// Result: "InventoryApi-CheckStock-HighLoad"

// Clear all state after tearing down the pipeline
generator.Clear();
```

## Notes

- **Normalization rules**: Service names and other inputs containing characters outside `[a-zA-Z0-9]` are normalized to dashes. Consecutive special characters collapse into a single dash, and leading/trailing dashes are trimmed.
- **Uniqueness guarantees**: The generator maintains internal counters per service–policy-type combination. When `RegisterName` is used, that specific string is reserved and will not be emitted by `GenerateName` until unregistered.
- **Thread safety**: The test suite implies that `RegisterName`, `UnregisterName`, `GenerateName`, and `Clear` operate on shared mutable state. Consumers using the generator concurrently must synchronize access externally; the tests themselves run sequentially.
- **Length constraints**: `IsValidPolicyName` enforces minimum and maximum lengths. Names that are too short or too long are rejected regardless of character validity.
- **Clearing state**: `Clear` resets all counters and the registration set. After clearing, previously registered names can be generated again, and counters start from their initial values.
- **Descriptive name parts**: `GenerateDescriptiveName` follows a fixed template. If any optional part (e.g., purpose) is null or whitespace, that segment and its preceding delimiter are omitted to avoid dangling dashes.
