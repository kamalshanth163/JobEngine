namespace WorkerService.Services;

// Lightweight heartbeat loop for the worker process.
// The full worker-heartbeat requeue flow is not wired up yet, so this keeps
// the service buildable without referencing missing persistence types.
public sealed class HeartbeatService(ILogger<HeartbeatService> _log) : BackgroundService
{
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(15);
    private readonly string _workerId =
        Environment.MachineName + "-" + Guid.NewGuid().ToString("N")[..6];

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            _log.LogInformation("Worker heartbeat pulse from {WorkerId} at {Time}",
                _workerId, DateTimeOffset.UtcNow);
            await Task.Delay(ScanInterval, ct);
        }
    }
}