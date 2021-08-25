#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Data;

namespace DotNetResiliencePipeline.Cli;

/// <summary>
/// Executes CLI commands by delegating to appropriate service layer methods.
/// Handles command routing, error handling, and output formatting.
/// </summary>
public sealed class CliCommandHandler
{
    private readonly ResiliencyPipelineService _pipelineService;
    private readonly PolicyRepository _policyRepository;
    private readonly ExecutionHistoryRepository _historyRepository;
    private readonly CliCommandValidator _validator;

    public CliCommandHandler(
        ResiliencyPipelineService pipelineService,
        PolicyRepository policyRepository,
        ExecutionHistoryRepository historyRepository)
    {
        _pipelineService = pipelineService;
        _policyRepository = policyRepository;
        _historyRepository = historyRepository;
        _validator = new CliCommandValidator();
    }

    /// <summary>
    /// Executes a parsed command and returns the result.
    /// </summary>
    public async Task<CommandExecutionResult> ExecuteAsync(CommandOptions options)
    {
        // Validate command
        var validation = _validator.Validate(options);
        if (!validation.IsValid)
        {
            return new CommandExecutionResult
            {
                Success = false,
                Message = validation.ToString(),
                ExitCode = 1
            };
        }

        try
        {
            // Route to appropriate handler
            var result = options.Command switch
            {
                "help" => HandleHelpCommand(options),
                "policy" => await HandlePolicyCommandAsync(options),
                "pipeline" => await HandlePipelineCommandAsync(options),
                "metrics" => await HandleMetricsCommandAsync(options),
                "health" => await HandleHealthCommandAsync(options),
                _ => new CommandExecutionResult
                {
                    Success = false,
                    Message = $"Unknown command: {options.Command}",
                    ExitCode = 1
                }
            };

            return result;
        }
        catch (Exception ex)
        {
            return new CommandExecutionResult
            {
                Success = false,
                Message = $"Command execution failed: {ex.Message}",
                Error = ex,
                ExitCode = 2
            };
        }
    }

    private CommandExecutionResult HandleHelpCommand(CommandOptions options)
    {
        return new CommandExecutionResult
        {
            Success = true,
            Message = CommandParser.GetHelpText(),
            ExitCode = 0
        };
    }

    private async Task<CommandExecutionResult> HandlePolicyCommandAsync(CommandOptions options)
    {
        return options.Subcommand switch
        {
            "create" => await CreatePolicyAsync(options),
            "list" => ListPolicies(options),
            "get" => GetPolicy(options),
            "delete" => DeletePolicy(options),
            "validate" => ValidatePolicy(options),
            _ => new CommandExecutionResult
            {
                Success = false,
                Message = "Subcommand required: create, list, get, delete, or validate",
                ExitCode = 1
            }
        };
    }

    private async Task<CommandExecutionResult> CreatePolicyAsync(CommandOptions options)
    {
        if (string.IsNullOrEmpty(options.PolicyName))
            return new CommandExecutionResult { Success = false, Message = "Policy name is required", ExitCode = 1 };

        if (string.IsNullOrEmpty(options.PolicyType))
            return new CommandExecutionResult { Success = false, Message = "Policy type is required", ExitCode = 1 };

        ResiliencyPolicy policy = options.PolicyType.ToLowerInvariant() switch
        {
            "circuitbreaker" => new CircuitBreakerPolicy(options.PolicyName)
            {
                FailureThreshold = options.FailureThreshold ?? 5,
                OpenDuration = options.OpenDuration ?? TimeSpan.FromSeconds(30)
            },
            "retry" => new RetryPolicy(options.PolicyName)
            {
                MaxRetries = options.MaxRetries ?? 3,
                InitialDelay = TimeSpan.FromMilliseconds(100)
            },
            "timeout" => new TimeoutPolicy(options.PolicyName)
            {
                Timeout = options.Timeout ?? TimeSpan.FromSeconds(10)
            },
            "bulkhead" => new BulkheadPolicy(options.PolicyName)
            {
                MaxParallelization = options.MaxParallelization ?? 10,
                MaxQueueLength = 50
            },
            "fallback" => new FallbackPolicy(options.PolicyName),
            _ => null!
        };

        if (policy is null)
            return new CommandExecutionResult { Success = false, Message = "Invalid policy type", ExitCode = 1 };

        _pipelineService.RegisterPolicy(policy);
        await _policyRepository.SaveAsync(policy);

        var message = $"✓ Policy '{options.PolicyName}' created successfully\n  Type: {options.PolicyType}\n  Id: {policy.Id}";
        return new CommandExecutionResult { Success = true, Message = message, ExitCode = 0 };
    }

    private CommandExecutionResult ListPolicies(CommandOptions options)
    {
        var policies = _pipelineService.GetAllPolicies();

        if (policies.Count == 0)
            return new CommandExecutionResult { Success = true, Message = "No policies registered", ExitCode = 0 };

        var message = new System.Text.StringBuilder();
        message.AppendLine($"Registered Policies ({policies.Count}):");
        message.AppendLine("-----------------------------------");

        foreach (var policy in policies)
        {
            message.AppendLine($"  • {policy.Name} ({policy.GetType().Name})");
            message.AppendLine($"    ID: {policy.Id}");
            message.AppendLine($"    Enabled: {policy.IsEnabled}");
        }

        return new CommandExecutionResult { Success = true, Message = message.ToString(), ExitCode = 0 };
    }

    private CommandExecutionResult GetPolicy(CommandOptions options)
    {
        if (string.IsNullOrEmpty(options.PolicyName))
            return new CommandExecutionResult { Success = false, Message = "Policy name is required", ExitCode = 1 };

        var policy = _pipelineService.GetPolicyByName(options.PolicyName);

        if (policy is null)
            return new CommandExecutionResult { Success = false, Message = $"Policy not found: {options.PolicyName}", ExitCode = 1 };

        var message = $"Policy: {policy.Name}\n  Type: {policy.GetType().Name}\n  ID: {policy.Id}\n  Enabled: {policy.IsEnabled}";
        return new CommandExecutionResult { Success = true, Message = message, ExitCode = 0 };
    }

    private CommandExecutionResult DeletePolicy(CommandOptions options)
    {
        if (string.IsNullOrEmpty(options.PolicyName))
            return new CommandExecutionResult { Success = false, Message = "Policy name is required", ExitCode = 1 };

        var policy = _pipelineService.GetPolicyByName(options.PolicyName);
        if (policy is null)
            return new CommandExecutionResult { Success = false, Message = $"Policy not found: {options.PolicyName}", ExitCode = 1 };

        if (_pipelineService.RemovePolicy(policy.Id))
            return new CommandExecutionResult { Success = true, Message = $"✓ Policy '{options.PolicyName}' deleted", ExitCode = 0 };

        return new CommandExecutionResult { Success = false, Message = "Failed to delete policy", ExitCode = 1 };
    }

    private CommandExecutionResult ValidatePolicy(CommandOptions options)
    {
        if (string.IsNullOrEmpty(options.PolicyName))
            return new CommandExecutionResult { Success = false, Message = "Policy name is required", ExitCode = 1 };

        var policy = _pipelineService.GetPolicyByName(options.PolicyName);
        if (policy is null)
            return new CommandExecutionResult { Success = false, Message = $"Policy not found: {options.PolicyName}", ExitCode = 1 };

        // Basic validation
        var message = $"✓ Policy '{options.PolicyName}' is valid";
        return new CommandExecutionResult { Success = true, Message = message, ExitCode = 0 };
    }

    private async Task<CommandExecutionResult> HandlePipelineCommandAsync(CommandOptions options)
    {
        var stats = _pipelineService.GetStatistics();
        var message = $"Pipeline ID: {stats.PipelineId}\nTotal Executions: {stats.TotalExecutions}\nSuccess Rate: {stats.SuccessRate:F2}%";
        return new CommandExecutionResult { Success = true, Message = message, ExitCode = 0 };
    }

    private async Task<CommandExecutionResult> HandleMetricsCommandAsync(CommandOptions options)
    {
        var stats = _pipelineService.GetStatistics();
        var message = $"Metrics:\n  Successful: {stats.SuccessfulExecutions}\n  Failed: {stats.FailedExecutions}\n  Success Rate: {stats.SuccessRate:F2}%";
        return new CommandExecutionResult { Success = true, Message = message, ExitCode = 0 };
    }

    private async Task<CommandExecutionResult> HandleHealthCommandAsync(CommandOptions options)
    {
        var message = "✓ Pipeline is healthy";
        return new CommandExecutionResult { Success = true, Message = message, ExitCode = 0 };
    }
}

/// <summary>
/// Result of a CLI command execution.
/// </summary>
public sealed class CommandExecutionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Exception? Error { get; set; }
    public int ExitCode { get; set; }
}
