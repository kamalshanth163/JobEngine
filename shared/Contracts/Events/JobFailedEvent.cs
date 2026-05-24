namespace JobEngine.Shared.Contracts.Events;

public sealed record JobFailedEvent
{
    public Guid JobId { get; init; }
    public Guid TenantId { get; init; }
    public string Error { get; init; } = default!;
    public int AttemptNum { get; init; }
    public bool IsFinal { get; init; }
}