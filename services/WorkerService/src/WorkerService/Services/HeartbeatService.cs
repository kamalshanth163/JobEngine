using MassTransit.Contracts.JobService;

namespace WorkerService.Services;

// Runs every 15s — sends heartbeat so HeartbeatMonitor knows we're alive.
// Scans for jobs locked by dead workers and requeues them.
public sealed class HeartbeatService(IServiceScopeFactory _factory,
    ILogger<HeartbeatService> _log) : BackgroundService
{
    private static readonly TimeSpan WorkerTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(15);
    private readonly string _workerId =
        Environment.MachineName + "-" + Guid.NewGuid().ToString("N")[..6];

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await using var scope = _factory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<JobsDbContext>();

            // Pulse our own heartbeat
            var hb = await db.WorkerHeartbeats
                .FirstOrDefaultAsync(w => w.WorkerId == _workerId, ct);
            if (hb is null)
            {
                db.WorkerHeartbeats.Add(WorkerHeartbeat.Register(_workerId));
            }
            else
            {
                hb.Pulse();
            }

            // Rescue orphaned jobs from dead workers
            var deadWorkers = await db.WorkerHeartbeats
                .Where(w => w.LastSeen < DateTime.UtcNow - WorkerTimeout)
                .Select(w => w.WorkerId).ToListAsync(ct);

            if (deadWorkers.Count > 0)
            {
                var orphans = await db.Jobs
                    .Where(j => j.Status == JobStatus.Running
                              && deadWorkers.Contains(j.WorkerId!))
                    .ToListAsync(ct);

                foreach (var job in orphans)
                {
                    job.MarkFailed("Worker died — auto-requeued");
                    _log.LogWarning("Rescued orphaned job {Id}", job.Id);
                }
                db.WorkerHeartbeats.RemoveRange(
                    db.WorkerHeartbeats.Where(w => deadWorkers.Contains(w.WorkerId)));
            }

            await db.SaveChangesAsync(ct);
            await Task.Delay(ScanInterval, ct);
        }
    }
}