namespace WorkerService.Services;

// Called by JobSubmittedConsumer to update job state in the DB.
// Scoped service — one instance per message consumed.
public sealed class JobStatusUpdater(
    JobsDbContext _ctx,
    ILogger<JobStatusUpdater> _log
) : IJobStatusUpdater
{
    public async Task MarkRunningAsync(Guid jobId, string workerId, CancellationToken ct)
    {
        var job = await _ctx.Jobs.FindAsync([jobId], ct)
            ?? throw new InvalidOperationException($"Job {jobId} not found");
        job.MarkRunning(workerId);
        await _ctx.SaveChangesAsync(ct);
        _log.LogInformation("Job {Id} claimed by {Worker}", jobId, workerId);
    }

    public async Task MarkCompletedAsync(Guid jobId, string? result, CancellationToken ct)
    {
        var job = await _ctx.Jobs.FindAsync([jobId], ct);
        job?.MarkCompleted(result);
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(Guid jobId, string error, CancellationToken ct)
    {
        var job = await _ctx.Jobs.FindAsync([jobId], ct);
        job?.MarkFailed(error);
        await _ctx.SaveChangesAsync(ct);
    }
}

public interface IJobStatusUpdater
{
    Task MarkRunningAsync(Guid jobId, string workerId, CancellationToken ct);
    Task MarkCompletedAsync(Guid jobId, string? result, CancellationToken ct);
    Task MarkFailedAsync(Guid jobId, string error, CancellationToken ct);
}