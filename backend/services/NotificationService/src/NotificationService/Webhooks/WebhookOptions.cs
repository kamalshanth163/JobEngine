namespace NotificationService.Webhooks;

public sealed class WebhookOptions
{
    public const string SectionName = "Webhooks";

    public List<WebhookEndpointOptions> Endpoints { get; init; } = [];
}

public sealed class WebhookEndpointOptions
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public Guid TenantId { get; init; }
    public string Url { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;
    public string[] Events { get; init; } = [];
    public bool IsActive { get; init; } = true;
}