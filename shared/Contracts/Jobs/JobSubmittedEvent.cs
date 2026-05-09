// Shared contracts live here so ALL services agree on the message shape.
// If Job Service publishes this, Worker Service consumes the exact same type.
// Never define message contracts inside a single service — that creates coupling.

namespace JobEngine.Shared.Contracts.Jobs;

// Record = immutable by default, perfect for message contracts
public sealed record JobSubmittedEvent
{
    public Guid JobId { get; init; }
    public Guid TenantId { get; init; }
    public string JobType { get; init; } = default!;
    public string Payload { get; init; } = "{}";
    public int Priority { get; init; }
    public int MaxAttempts { get; init; } = 3;
    public DateTime SubmittedAt { get; init; } = DateTime.UtcNow;
}

public sealed record JobCompletedEvent
{
    public Guid JobId { get; init; }
    public Guid TenantId { get; init; }
    public string? Result { get; init; }
    public DateTime CompletedAt { get; init; }
}

public sealed record JobFailedEvent
{
    public Guid JobId { get; init; }
    public Guid TenantId { get; init; }
    public string Error { get; init; } = default!;
    public int AttemptNum { get; init; }
    public bool IsFinal { get; init; } // true = dead letter
}