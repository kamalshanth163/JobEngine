namespace JobService.Domain.Entities;

public enum JobStatus
{
    Pending,   // Created, waiting to be queued
    Queued,    // Published to RabbitMQ
    Running,   // Worker claimed it
    Completed, // Finished successfully
    Failed,    // Failed, will retry
    Retrying,  // Waiting for next retry attempt
    DeadLetter // Exhausted all retries
}

public sealed class Job
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Type { get; private set; } = default!;
    public string Payload { get; private set; } = "{}";
    public JobStatus Status { get; private set; }
    public int Priority { get; private set; }
    public int Attempt { get; private set; }
    public int MaxAttempts { get; private set; }
    public string? WorkerId { get; private set; }
    public string? Error { get; private set; }
    public string? Result { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Job() { }

    public static Job Create(Guid tenantId, string type,
        string payload, int priority = 0, int maxAttempts = 3,
        DateTime? scheduledAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        return new Job
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = type,
            Payload = payload,
            Status = JobStatus.Pending,
            Priority = priority,
            MaxAttempts = maxAttempts,
            ScheduledAt = scheduledAt ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Each transition validates the current state — impossible to skip steps
    public void MarkQueued()
    {
        EnsureStatus(JobStatus.Pending, "queue");
        Status = JobStatus.Queued;
    }

    public void MarkRunning(string workerId)
    {
        EnsureStatus(JobStatus.Queued, "start");
        Status = JobStatus.Running;
        WorkerId = workerId;
        StartedAt = DateTime.UtcNow;
        Attempt++;
    }

    public void MarkCompleted(string? result = null)
    {
        EnsureStatus(JobStatus.Running, "complete");
        Status = JobStatus.Completed;
        Result = result;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        EnsureStatus(JobStatus.Running, "fail");
        Error = error;
        // Auto-decide: retry or dead letter based on attempt count
        Status = Attempt < MaxAttempts
            ? JobStatus.Retrying
            : JobStatus.DeadLetter;
        CompletedAt = DateTime.UtcNow;
    }

    public void RequeueForRetry()
    {
        EnsureStatus(JobStatus.Retrying, "requeue");
        Status = JobStatus.Queued;
        WorkerId = null;
        StartedAt = null;
    }

    private void EnsureStatus(JobStatus expected, string action)
    {
        if (Status != expected)
            throw new InvalidOperationException(
                $"Cannot {action} job in state {Status}. Expected {expected}.");
    }
}