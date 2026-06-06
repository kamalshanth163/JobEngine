using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ExecutionService.Core.Handlers;

public sealed class GenerateReportHandler(ILogger<GenerateReportHandler> log) : IJobHandler
{
    public string JobType => "generate-report";

    public async Task<string?> HandleAsync(string payload, CancellationToken ct)
    {
        var req = JsonSerializer.Deserialize<GenerateReportPayload>(payload)
            ?? throw new ArgumentException("Invalid payload");

        log.LogInformation("Generating report {ReportType} for tenant {TenantId}", req.ReportType, req.TenantId);

        await Task.Delay(400, ct);

        return $"Report '{req.ReportType}' generated for tenant '{req.TenantId}'";
    }
}

public sealed record GenerateReportPayload(string TenantId, string ReportType);