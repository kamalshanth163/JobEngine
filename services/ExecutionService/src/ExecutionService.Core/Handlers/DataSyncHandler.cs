using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ExecutionService.Core.Handlers;

public sealed class DataSyncHandler(ILogger<DataSyncHandler> log) : IJobHandler
{
    public string JobType => "data-sync";

    public async Task<string?> HandleAsync(string payload, CancellationToken ct)
    {
        var req = JsonSerializer.Deserialize<DataSyncPayload>(payload)
            ?? throw new ArgumentException("Invalid payload");

        log.LogInformation("Running data sync from {Source} to {Destination}", req.SourceSystem, req.DestinationSystem);

        await Task.Delay(600, ct);

        return $"Data sync complete: {req.SourceSystem} -> {req.DestinationSystem}";
    }
}

public sealed record DataSyncPayload(string SourceSystem, string DestinationSystem);