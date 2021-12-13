# DotNet Resilience Pipeline

![CI](https://github.com/sarmkadan/dotnet-resilience-pipeline/actions/workflows/ci.yml/badge.svg)
![License](https://img.shields.io/github/license/sarmkadan/dotnet-resilience-pipeline)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)

A comprehensive, production-grade resilience library for .NET applications featuring circuit breaker, bulkhead, retry, timeout, and fallback patterns with fluent configuration and built-in observability.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Policy Types](#policy-types)
- [Configuration](#configuration)
- [Examples](#examples)
- [API Reference](#api-reference)
- [Monitoring & Metrics](#monitoring--metrics)
- [Circuit Breaker Dashboard](#circuit-breaker-dashboard)
- [Failure Injection Testing](#failure-injection-testing)
- [Resilience Metrics Export](#resilience-metrics-export)
- [Deployment](#deployment)
- [Troubleshooting](#troubleshooting)
- [Testing](#testing)
- [Benchmarks](#benchmarks)
- [Related Projects](#related-projects)
- [Contributing](#contributing)
- [License](#license)

## ...

## PoliciesControllerExtensions

The `PoliciesControllerExtensions` class provides a set of extension methods for working with policy-related operations. It enables creating, retrieving, validating, and checking the existence of policies.

### Example Usage
```csharp
using Resilience.Api;
using Resilience.Dtos;

// Create a policy
var policy = new PolicyDto { /* initialize properties */ };
var createResponse = await PoliciesControllerExtensions.CreatePolicyAsync(policy);
Console.WriteLine(createResponse.ToJson());

// Get all policies
var policies = await PoliciesControllerExtensions.GetAllPoliciesListAsync();
foreach (var p in policies)
{
    Console.WriteLine(p.ToJson());
}

// Get a policy by id
var policyId = Guid.NewGuid();
var policyById = await PoliciesControllerExtensions.GetPolicyAsync<PolicyDto>(policyId);
Console.WriteLine(policyById?.ToJson());

// Validate policy configuration
var validationResult = await PoliciesControllerExtensions.ValidatePolicyConfigurationAsync(policy);
Console.WriteLine(validationResult.ToJson());

// Check if policy exists
var exists = await PoliciesControllerExtensions.PolicyExistsAsync(policyId);
Console.WriteLine(exists);
```
## ...
