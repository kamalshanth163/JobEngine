using JobService.Infrastructure.Persistence;
using JobService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
        var job = await _ctx.Jobs.IgnoreQueryFilters()
            .FirstOrDefaultAsync(j => j.Id == jobId, ct)
            ?? throw new InvalidOperationException($"Job {jobId} not found");
        job.MarkRunning(workerId);
        await _ctx.SaveChangesAsync(ct);
        _log.LogInformation("Job {Id} claimed by {Worker}", jobId, workerId);
    }

    public async Task MarkCompletedAsync(Guid jobId, string? result, CancellationToken ct)
    {
        var job = await _ctx.Jobs.IgnoreQueryFilters()
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);
        job?.MarkCompleted(result);
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task<JobFailureState> MarkFailedAsync(Guid jobId, string error, CancellationToken ct)
    {
        var job = await _ctx.Jobs.IgnoreQueryFilters()
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job is null)
            throw new InvalidOperationException($"Job {jobId} not found");

        job.MarkFailed(error);
        await _ctx.SaveChangesAsync(ct);

        var isFinal = job.Status == JobStatus.DeadLetter;
        return new JobFailureState(job.Attempt, job.MaxAttempts, isFinal);
    }
}

public interface IJobStatusUpdater
{
    Task MarkRunningAsync(Guid jobId, string workerId, CancellationToken ct);
    Task MarkCompletedAsync(Guid jobId, string? result, CancellationToken ct);
    Task<JobFailureState> MarkFailedAsync(Guid jobId, string error, CancellationToken ct);
}

public sealed record JobFailureState(int Attempt, int MaxAttempts, bool IsFinal);