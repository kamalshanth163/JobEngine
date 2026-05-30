namespace NotificationService.Webhooks;

public interface IWebhookRepository
{
    Task<IReadOnlyList<WebhookEndpoint>> GetActiveForTenantAsync(
        Guid tenantId,
        string eventType,
        CancellationToken ct);
}

public sealed record WebhookEndpoint(
    Guid Id,
    Guid TenantId,
    string Url,
    string Secret,
    string[] Events,
    bool IsActive = true);