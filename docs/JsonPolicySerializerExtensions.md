# JsonPolicySerializerExtensions
The `JsonPolicySerializerExtensions` class provides a set of static methods for serializing and deserializing JSON policies. It allows for customization of the serialization process through options and supports both single and multiple policy serialization. This class is designed to be used in conjunction with the resilience pipeline to handle policy serialization and deserialization in a flexible and efficient manner.

## API
* `public static JsonPolicySerializer WithOptions`: Returns a `JsonPolicySerializer` instance with the specified options. This method allows for customization of the serialization process.
* `public static bool TryDeserialize`: Attempts to deserialize a JSON string into a policy. Returns `true` if deserialization is successful, `false` otherwise.
* `public static string Serialize`: Serializes a policy into a JSON string. This method is used to convert a policy into a JSON representation.
* `public static string SerializeMultiple`: Serializes multiple policies into a JSON string. This method is used to convert multiple policies into a single JSON representation.

## Usage
The following examples demonstrate how to use the `JsonPolicySerializerExtensions` class:
```csharp
// Example 1: Serializing a single policy
var policy = new Policy(); // assume Policy is a class representing a policy
var json = JsonPolicySerializerExtensions.Serialize(policy);
Console.WriteLine(json);

// Example 2: Deserializing multiple policies
var jsonMultiple = "[{\"policy1\":\"value1\"},{\"policy2\":\"value2\"}]";
var policies = JsonPolicySerializerExtensions.TryDeserialize(jsonMultiple, out var deserializedPolicies);
if (policies)
{
    foreach (var policy in deserializedPolicies)
    {
        Console.WriteLine(policy);
    }
}
```

## Notes
When using the `JsonPolicySerializerExtensions` class, note that the `TryDeserialize` method returns `false` if the deserialization process fails, and the `Serialize` and `SerializeMultiple` methods may throw exceptions if the serialization process fails. Additionally, the `WithOptions` method allows for customization of the serialization process, but the options must be valid and consistent with the serialization process. The `JsonPolicySerializerExtensions` class is thread-safe, as it only contains static methods and does not maintain any state. However, the underlying serialization and deserialization processes may not be thread-safe, depending on the implementation of the `JsonPolicySerializer` class.
