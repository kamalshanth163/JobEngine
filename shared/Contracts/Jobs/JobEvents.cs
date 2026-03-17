namespace JobEngine.Shared.Contracts.Jobs;

// These are the messages that flow through RabbitMQ between services.
// 'record' = immutable by default, perfect for message contracts.
// 'init' setters = can only be set during object initialisation.
// NEVER change property names without versioning — consumers will break.

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