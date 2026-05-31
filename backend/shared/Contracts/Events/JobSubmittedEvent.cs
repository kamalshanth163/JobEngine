namespace JobEngine.Shared.Contracts.Events;

public sealed record JobSubmittedEvent
{
    public Guid JobId { get; init; }
    public Guid TenantId { get; init; }
    public string JobType { get; init; } = default!;
    public string Payload { get; init; } = "{}";
    public int Priority { get; init; }
    public int MaxAttempts { get; init; } = 3;
    public DateTime SubmittedAt { get; init; } = DateTime.UtcNow;
    public string? WebhookUrl { get; init; }
    public string? WebhookSecret { get; init; }
}