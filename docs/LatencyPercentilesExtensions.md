# LatencyPercentilesExtensions

This document describes the `LatencyPercentilesExtensions` class and the `LatencyPercentiles` struct used in the DotNetResiliencePipeline.Data namespace.

## Overview

The `LatencyPercentilesExtensions` class provides extension methods for working with latency percentile data. The `LatencyPercentiles` struct (defined in `ExecutionHistoryRepository.cs`) represents computed P50, P90, and P99 latency values.

## Methods

### ToDictionary

Converts a `LatencyPercentiles` instance to a read-only dictionary with string keys representing percentile names and double values representing the latency in milliseconds.

#### Signature

```csharp
public static IReadOnlyDictionary<string, double> ToDictionary(this LatencyPercentiles percentiles)
```

#### Parameters

- `percentiles`: The `LatencyPercentiles` instance to convert.

#### Return Value

A read-only dictionary containing three entries:
- Key "p50" → 50th percentile (median) latency
- Key "p90" → 90th percentile latency  
- Key "p99" → 99th percentile latency

#### Example Usage

```csharp
using DotNetResiliencePipeline.Data;

// Assume we have an ExecutionHistoryRepository instance
var repository = new ExecutionHistoryRepository();
// ... add some execution records ...

// Get latency percentiles
LatencyPercentiles percentiles = repository.GetLatencyPercentiles();

// Convert to dictionary for easy serialization or inspection
IReadOnlyDictionary<string, double> dict = percentiles.ToDictionary();

// Access individual values
double p50 = dict["p50"]; // 50th percentile
double p90 = dict["p90"]; // 90th percentile
double p99 = dict["p99"]; // 99th percentile

// Or iterate through all percentiles
foreach (var kvp in dict)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}ms");
}
```

## LatencyPercentiles Struct

Defined in `ExecutionHistoryRepository.cs`, this struct holds three latency percentile values:

### Properties

- `P50`: 50th percentile (median) latency in milliseconds
- `P90`: 90th percentile latency in milliseconds  
- `P99`: 99th percentile latency in milliseconds

### Calculation Method

The percentiles are calculated by `ExecutionHistoryRepository.GetLatencyPercentiles()` using the nearest-rank method on recorded execution times.

## Dictionary Key Names

The `ToDictionary` method uses the following fixed keys:
- `"p50"` for the 50th percentile
- `"p90"` for the 90th percentile
- `"p99"` for the 99th percentile

These keys are consistent across all implementations and should be used when accessing percentile values from the dictionary.

## Complete Example

```csharp
using System;
using System.Collections.Generic;
using DotNetResiliencePipeline.Data;
using DotNetResiliencePipeline.Exceptions;

public class LatencyPercentilesExample
{
    public static void Main()
    {
        // Create repository with default retention (60 minutes)
        var repo = new ExecutionHistoryRepository();
        
        // Simulate some operation executions
        var random = new Random();
        for (int i = 0; i < 100; i++)
        {
            var record = new ExecutionRecord
            {
                PolicyName = "ExamplePolicy",
                PolicyId = "example-policy-id",
                IsSuccess = true,
                ExecutionTimeMs = random.Next(50, 500), // 50-500ms
                AttemptCount = 1
            };
            
            repo.Record(record);
        }
        
        // Get latency percentiles
        LatencyPercentiles percentiles = repo.GetLatencyPercentiles();
        
        // Convert to dictionary
        IReadOnlyDictionary<string, double> dict = percentiles.ToDictionary();
        
        // Display results
        Console.WriteLine("Latency Percentiles:");
        Console.WriteLine($"P50 (Median): {dict["p50"]:F2}ms");
        Console.WriteLine($"P90: {dict["p90"]:F2}ms");
        Console.WriteLine($"P99: {dict["p99"]:F2}ms");
        
        // Verify all expected keys are present
        Console.WriteLine("\nDictionary contents:");
        foreach (var kvp in dict)
        {
            Console.WriteLine($"{kvp.Key.ToUpper()}: {kvp.Value:F2}ms");
        }
    }
}
```

This example demonstrates:
1. Creating an `ExecutionHistoryRepository`
2. Recording sample execution data
3. Retrieving latency percentiles
4. Converting to dictionary using the extension method
5. Accessing and displaying the percentile values