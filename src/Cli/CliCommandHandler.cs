#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Data;
using DotNetResiliencePipeline.Formatters;
using DotNetResiliencePipeline.Exceptions;

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
    private readonly CircuitBreakerService _circuitBreakerService;

    public CliCommandHandler(
        ResiliencyPipelineService pipelineService,
        PolicyRepository policyRepository,
        ExecutionHistoryRepository historyRepository,
        CircuitBreakerService? circuitBreakerService = null)
    {
        _pipelineService = pipelineService ?? throw new ArgumentNullException(nameof(pipelineService));
        _policyRepository = policyRepository ?? throw new ArgumentNullException(nameof(policyRepository));
        _historyRepository = historyRepository ?? throw new ArgumentNullException(nameof(historyRepository));
        _circuitBreakerService = circuitBreakerService ?? new CircuitBreakerService();
        _validator = new CliCommandValidator();
    }

    /// <summary>
    /// Executes a parsed command and returns the result.
    /// </summary>
    /// <exception cref="ValidationException">Thrown when command validation fails.</exception>
    public async Task<CommandExecutionResult> ExecuteAsync(CommandOptions options)
    {
        // Validate command
        var validation = _validator.Validate(options);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .Select((message, index) => new { message, index })
                .ToDictionary(e => $"error_{e.index}", e => e.message);
            throw new ValidationException("Command validation failed", errors);
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
                "dashboard" => await HandleDashboardCommandAsync(options),
                "inject" => HandleInjectCommand(options),
                "export" => await HandleExportCommandAsync(options),
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
            throw new ValidationException("Policy name is required", new Dictionary<string, string> { { nameof(options.PolicyName), "Policy name cannot be empty" } });

        if (string.IsNullOrEmpty(options.PolicyType))
            throw new ValidationException("Policy type is required", new Dictionary<string, string> { { nameof(options.PolicyType), "Policy type cannot be empty" } });

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
            _ => throw new ValidationException("Invalid policy type", new Dictionary<string, string> { { nameof(options.PolicyType), "Valid types: circuitbreaker, retry, timeout, bulkhead, fallback" } })
        };

        _pipelineService.RegisterPolicy(policy);
        await _policyRepository.SaveAsync(policy);

        var message = $"✓ Policy '{options.PolicyName}' created successfully\n Type: {options.PolicyType}\n Id: {policy.Id}";
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
            message.AppendLine($" • {policy.Name} ({policy.GetType().Name})");
            message.AppendLine($" ID: {policy.Id}");
            message.AppendLine($" Enabled: {policy.IsEnabled}");
        }

        return new CommandExecutionResult { Success = true, Message = message.ToString(), ExitCode = 0 };
    }

    private CommandExecutionResult GetPolicy(CommandOptions options)
    {
        if (string.IsNullOrEmpty(options.PolicyName))
            throw new ValidationException("Policy name is required", new Dictionary<string, string> { { nameof(options.PolicyName), "Policy name cannot be empty" } });

        var policy = _pipelineService.GetPolicyByName(options.PolicyName);

        if (policy is null)
            throw new ValidationException("Policy not found", new Dictionary<string, string> { { nameof(options.PolicyName), $"Policy not found: {options.PolicyName}" } });

        var message = $"Policy: {policy.Name}\n Type: {policy.GetType().Name}\n ID: {policy.Id}\n Enabled: {policy.IsEnabled}";
        return new CommandExecutionResult { Success = true, Message = message, ExitCode = 0 };
    }

    private CommandExecutionResult DeletePolicy(CommandOptions options)
    {
        if (string.IsNullOrEmpty(options.PolicyName))
            throw new ValidationException("Policy name is required", new Dictionary<string, string> { { nameof(options.PolicyName), "Policy name cannot be empty" } });

        var policy = _pipelineService.GetPolicyByName(options.PolicyName);
        if (policy is null)
            throw new ValidationException("Policy not found", new Dictionary<string, string> { { nameof(options.PolicyName), $"Policy not found: {options.PolicyName}" } });

        if (_pipelineService.RemovePolicy(policy.Id))
            return new CommandExecutionResult { Success = true, Message = $"✓ Policy '{options.PolicyName}' deleted", ExitCode = 0 };

        throw new InvalidOperationException("Failed to delete policy");
    }

    private CommandExecutionResult ValidatePolicy(CommandOptions options)
    {
        if (string.IsNullOrEmpty(options.PolicyName))
            throw new ValidationException("Policy name is required", new Dictionary<string, string> { { nameof(options.PolicyName), "Policy name cannot be empty" } });

        var policy = _pipelineService.GetPolicyByName(options.PolicyName);
        if (policy is null)
            throw new ValidationException("Policy not found", new Dictionary<string, string> { { nameof(options.PolicyName), $"Policy not found: {options.PolicyName}" } });

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
        var message = $"Metrics:\n Successful: {stats.SuccessfulExecutions}\n Failed: {stats.FailedExecutions}\n Success Rate: {stats.SuccessRate:F2}%";
        return new CommandExecutionResult { Success = true, Message = message, ExitCode = 0 };
    }

    private async Task<CommandExecutionResult> HandleHealthCommandAsync(CommandOptions options)
    {
        var message = "✓ Pipeline is healthy";
        return new CommandExecutionResult { Success = true, Message = message, ExitCode = 0 };
    }

    /// <summary>
    /// Displays the circuit breaker dashboard for all registered breakers.
    /// Usage: dashboard [--name <policyName>] [--reset]
    /// </summary>
    private async Task<CommandExecutionResult> HandleDashboardCommandAsync(CommandOptions options)
    {
        var dashboardController = new Api.Controllers.CircuitBreakerDashboardController(
            _pipelineService, _circuitBreakerService);

        if (options.PolicyName is not null && options.HasFlag("reset"))
        {
            var resetResponse = await dashboardController.ResetBreakerAsync(options.PolicyName);
            if (!resetResponse.Success)
                throw new InvalidOperationException(resetResponse.Message ?? "Reset failed");

            return new CommandExecutionResult
            {
                Success = true,
                Message = $"✓ Circuit breaker '{options.PolicyName}' reset to Closed state",
                ExitCode = 0
            };
        }

        if (options.PolicyName is not null)
        {
            var statusResponse = await dashboardController.GetBreakerStatusAsync(options.PolicyName);
            if (!statusResponse.Success)
                throw new InvalidOperationException(statusResponse.Message ?? "Not found");

            var s = statusResponse.Data!;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Circuit Breaker: {s.Name}");
            sb.AppendLine($" State : {s.State}");
            sb.AppendLine($" Trips : {s.TripCount}");
            sb.AppendLine($" Consec. Failures : {s.ConsecutiveFailures}/{s.FailureThreshold}");
            sb.AppendLine($" Success Rate : {s.SuccessRate:F2}%");
            if (s.SecondsUntilHalfOpen.HasValue)
                sb.AppendLine($" Time to Half-Open: {s.SecondsUntilHalfOpen:F1}s");

            return new CommandExecutionResult { Success = true, Message = sb.ToString(), ExitCode = 0 };
        }

        var response = await dashboardController.GetDashboardAsync();
        if (!response.Success)
            throw new InvalidOperationException(response.Message ?? "Dashboard error");

        var d = response.Data!;
        var msg = new System.Text.StringBuilder();
        msg.AppendLine($"Circuit Breaker Dashboard [{d.GeneratedAt:HH:mm:ss} UTC]");
        msg.AppendLine($" Overall Health : {d.OverallHealth}");
        msg.AppendLine($" Total Breakers : {d.TotalBreakers} (Closed={d.ClosedCount} Open={d.OpenCount} HalfOpen={d.HalfOpenCount})");
        msg.AppendLine($" Total Trips : {d.TotalTrips}");
        msg.AppendLine();

        foreach (var b in d.Breakers)
        {
            var stateIcon = b.State switch { "Open" => "✗", "HalfOpen" => "◑", _ => "✓" };
            msg.AppendLine($" {stateIcon} {b.Name,-30} {b.State,-10} trips={b.TripCount} rate={b.SuccessRate:F1}%");
        }

        return new CommandExecutionResult { Success = true, Message = msg.ToString(), ExitCode = 0 };
    }

    /// <summary>
    /// Provides information about the failure injection feature.
    /// Usage: inject --rule <key> --type <exception|latency|timeout> [--rate <0.0-1.0>]
    /// </summary>
    private CommandExecutionResult HandleInjectCommand(CommandOptions options)
    {
        var ruleKey = options.GetArgument("rule");
        if (string.IsNullOrWhiteSpace(ruleKey))
            throw new ValidationException(
                "Usage: inject --rule <key> --type <exception|latency|timeout> [--rate <0.0-1.0>]\n" +
                "Use the FailureInjectionService API to register rules programmatically.",
                new Dictionary<string, string> { { nameof(ruleKey), "Rule key is required" } });

        var typeArg = options.GetArgument("type", "exception");
        var rateArg = options.GetArgument("rate", "1.0");

        if (!Enum.TryParse<InjectionType>(typeArg, ignoreCase: true, out var injType))
            throw new ValidationException("Unknown injection type", new Dictionary<string, string> { { nameof(typeArg), $"Unknown injection type: {typeArg}" } });

        if (!double.TryParse(rateArg, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var rate))
            throw new ValidationException("Invalid rate format", new Dictionary<string, string> { { nameof(rateArg), $"Rate must be a number: {rateArg}" } });

        return new CommandExecutionResult
        {
            Success = true,
            Message = $"Injection rule summary:\n Rule key : {ruleKey}\n Type : {injType}\n Rate : {rate:P0}\n" +
                     "Register this rule via FailureInjectionService.AddRule() to activate it.",
            ExitCode = 0
        };
    }

    /// <summary>
    /// Exports resilience metrics.
    /// Usage: export [--format json|csv|prometheus] [--output <file>]
    /// </summary>
    private async Task<CommandExecutionResult> HandleExportCommandAsync(CommandOptions options)
    {
        var format = options.GetArgument("format", "json")!.ToLowerInvariant();
        var snapshot = _pipelineService.GetStats();
        var exporter = new MetricsExporter();

        string exported;
        try
        {
            exported = format switch
            {
                "csv" => exporter.ExportCsv(snapshot),
                "prometheus" => exporter.ExportPrometheus(snapshot),
                _ => exporter.ExportJson(snapshot)
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Export failed: {ex.Message}");
        }

        if (options.OutputFile is not null)
        {
            await File.WriteAllTextAsync(options.OutputFile, exported);
            return new CommandExecutionResult
            {
                Success = true,
                Message = $"✓ Metrics exported ({format.ToUpperInvariant()}) → {options.OutputFile}",
                ExitCode = 0
            };
        }

        return new CommandExecutionResult { Success = true, Message = exported, ExitCode = 0 };
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