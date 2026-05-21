using Microsoft.Extensions.Options;

namespace NotificationService.Webhooks;

public sealed class ConfigurationWebhookRepository(IOptionsMonitor<WebhookOptions> options) : IWebhookRepository
{
    public Task<IReadOnlyList<WebhookEndpoint>> GetActiveForTenantAsync(
        Guid tenantId,
        string eventType,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var endpoints = options.CurrentValue.Endpoints
            .Where(e => e.IsActive)
            .Where(e => e.TenantId == tenantId)
            .Where(e => e.Events.Any(x => string.Equals(x, eventType, StringComparison.OrdinalIgnoreCase)))
            .Where(e => Uri.TryCreate(e.Url, UriKind.Absolute, out _))
            .Select(e => new WebhookEndpoint(
                ParseId(e.Id),
                e.TenantId,
                e.Url,
                e.Secret,
                e.Events,
                e.IsActive))
            .ToArray();

        return Task.FromResult<IReadOnlyList<WebhookEndpoint>>(endpoints);
    }

    private static Guid ParseId(string value) =>
        Guid.TryParse(value, out var parsed) ? parsed : Guid.NewGuid();
}