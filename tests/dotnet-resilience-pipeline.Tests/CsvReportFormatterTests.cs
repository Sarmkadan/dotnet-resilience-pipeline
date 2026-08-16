using DotNetResiliencePipeline.Formatters;
using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Middleware;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public class CsvReportFormatterTests
{
    private readonly CsvReportFormatter _formatter;

    public CsvReportFormatterTests()
    {
        _formatter = new CsvReportFormatter();
    }

    [Fact]
    public void FormatPipelineMetrics_ValidInput_ReturnsCsvString()
    {
        var stats = new PipelineStatistics
        {
            PipelineId = "test-1",
            CreatedAt = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            TotalExecutions = 10,
            SuccessfulExecutions = 8,
            FailedExecutions = 2,
            SuccessRate = 80.0,
            PolicyCount = 1
        };

        var result = _formatter.FormatPipelineMetrics(stats);

        result.Should().Contain("Metric,Value");
        result.Should().Contain("Pipeline ID,test-1");
        result.Should().Contain("Total Executions,10");
        result.Should().Contain("Success Rate,80.00%");
    }

    [Fact]
    public void FormatExecutionHistory_EmptyList_ReturnsHeaderOnly()
    {
        var records = new List<ExecutionRecord>();

        var result = _formatter.FormatExecutionHistory(records);

        result.Should().Be("Timestamp,Policy Name,Success,Duration Ms,Status" + Environment.NewLine);
    }

    [Fact]
    public void FormatExecutionHistory_ValidList_ReturnsCsvString()
    {
        var records = new List<ExecutionRecord>
        {
            new() { Timestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc), PolicyName = "test-policy", IsSuccess = true, ExecutionTimeMs = 100 }
        };

        var result = _formatter.FormatExecutionHistory(records);

        result.Should().Contain("test-policy,True,100,Success");
    }

    [Fact]
    public async Task ExportToFileAsync_ValidInput_WritesToFile()
    {
        var content = "test content";
        var filePath = "test_report.csv";
        
        await _formatter.ExportToFileAsync(content, filePath);

        File.Exists(filePath).Should().BeTrue();
        var writtenContent = await File.ReadAllTextAsync(filePath);
        writtenContent.Should().Be(content);

        File.Delete(filePath); // Cleanup
    }

    [Fact]
    public void FormatLogs_HandlesSpecialCharacters_EscapesCorrectly()
    {
        var logs = new List<LogEntry>
        {
            new() { 
                Timestamp = DateTime.UtcNow, 
                PolicyName = "policy,name", 
                OperationName = "op\"name", 
                Success = true, 
                DurationMs = 50,
                Message = "line\nbreak"
            }
        };

        var result = _formatter.FormatLogs(logs);

        // Should contain quoted escaped versions
        result.Should().Contain("\"policy,name\"");
        result.Should().Contain("\"op\"\"name\"");
        result.Should().Contain("\"line\nbreak\"");
    }
}
