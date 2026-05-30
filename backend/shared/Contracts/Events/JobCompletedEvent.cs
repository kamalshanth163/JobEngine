namespace JobEngine.Shared.Contracts.Events;

public sealed record JobCompletedEvent
{
    public Guid JobId { get; init; }
    public Guid TenantId { get; init; }
    public string? Result { get; init; }
    public DateTime CompletedAt { get; init; }
}