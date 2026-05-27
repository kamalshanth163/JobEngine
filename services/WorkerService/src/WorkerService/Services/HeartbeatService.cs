namespace WorkerService.Services;

using JobEngine.Shared.Contracts.Events;
using JobService.Domain.Entities;
using JobService.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

public sealed class HeartbeatService(
    ILogger<HeartbeatService> _log,
    IConnectionMultiplexer _redis,
    IWorkerIdentity _identity,
    IServiceScopeFactory _scopeFactory
) : BackgroundService
{
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan HeartbeatTtl = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan StaleRunningThreshold = TimeSpan.FromMinutes(2);
    private const int RecoveryBatchSize = 100;

    private string HeartbeatKey => $"worker:heartbeat:{_identity.WorkerId}";

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _log.LogInformation("Heartbeat service started for worker {WorkerId}", _identity.WorkerId);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await PublishHeartbeatAsync(ct);
                await RecoverStaleRunningJobsAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogError(ex, "Heartbeat loop failed for worker {WorkerId}", _identity.WorkerId);
            }

            try
            {
                await Task.Delay(ScanInterval, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _log.LogInformation("Heartbeat service stopped for worker {WorkerId}", _identity.WorkerId);
    }

    private async Task PublishHeartbeatAsync(CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync(
            HeartbeatKey,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            HeartbeatTtl);

        _log.LogDebug("Heartbeat pulse from {WorkerId}", _identity.WorkerId);
    }

    private async Task RecoverStaleRunningJobsAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow - StaleRunningThreshold;

        using var scope = _scopeFactory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<JobsDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var staleCandidates = await ctx.Jobs
            .IgnoreQueryFilters()
            .Where(j => j.Status == JobStatus.Running
                && j.WorkerId != null
                && j.StartedAt != null
                && j.StartedAt < cutoff)
            .OrderBy(j => j.StartedAt)
            .Take(RecoveryBatchSize)
            .ToListAsync(ct);

        if (staleCandidates.Count == 0)
            return;

        var db = _redis.GetDatabase();

        foreach (var job in staleCandidates)
        {
            var ownerId = job.WorkerId;
            if (string.IsNullOrWhiteSpace(ownerId))
                continue;

            var ownerAlive = await db.KeyExistsAsync($"worker:heartbeat:{ownerId}");
            if (ownerAlive)
                continue;

            var error = $"Recovered from stale worker heartbeat timeout for {ownerId}.";
            job.MarkFailed(error);

            var isFinal = job.Status == JobStatus.DeadLetter;
            if (!isFinal)
                job.RequeueForRetry();

            await ctx.SaveChangesAsync(ct);

            await bus.Publish(new JobFailedEvent
            {
                JobId = job.Id,
                TenantId = job.TenantId,
                Error = error,
                AttemptNum = job.Attempt,
                IsFinal = isFinal
            }, ct);

            if (!isFinal)
            {
                await db.KeyDeleteAsync($"job:processed:{job.Id}");
                await db.KeyDeleteAsync($"lock:job:lock:{job.Id}");

                await bus.Publish(new JobSubmittedEvent
                {
                    JobId = job.Id,
                    TenantId = job.TenantId,
                    JobType = job.Type,
                    Payload = job.Payload,
                    Priority = job.Priority,
                    MaxAttempts = job.MaxAttempts,
                    SubmittedAt = DateTime.UtcNow
                }, ct);
            }

            _log.LogWarning(
                "Recovered stale running job {JobId} from worker {StaleWorker}. Requeued={Requeued}",
                job.Id,
                ownerId,
                !isFinal);
        }
    }
}