using Microsoft.Extensions.Options;
using NotificationService.Webhooks;

namespace NotificationService.Tests;

public sealed class ConfigurationWebhookRepositoryTests
{
    [Fact]
    public async Task GetActiveForTenantAsync_ReturnsOnlyActiveMatchingEndpoints()
    {
        var tenantId = Guid.NewGuid();
        var validEndpointId = Guid.NewGuid();
        var options = new WebhookOptions
        {
            Endpoints =
            [
                new WebhookEndpointOptions
                {
                    Id = validEndpointId.ToString(),
                    TenantId = tenantId,
                    Url = "https://example.test/webhooks/jobs",
                    Secret = "secret-1",
                    Events = ["job.completed", "job.failed"],
                    IsActive = true
                },
                new WebhookEndpointOptions
                {
                    TenantId = tenantId,
                    Url = "not-a-url",
                    Secret = "secret-2",
                    Events = ["job.completed"],
                    IsActive = true
                },
                new WebhookEndpointOptions
                {
                    TenantId = tenantId,
                    Url = "https://example.test/webhooks/inactive",
                    Secret = "secret-3",
                    Events = ["job.completed"],
                    IsActive = false
                },
                new WebhookEndpointOptions
                {
                    TenantId = Guid.NewGuid(),
                    Url = "https://example.test/webhooks/other-tenant",
                    Secret = "secret-4",
                    Events = ["job.completed"],
                    IsActive = true
                },
                new WebhookEndpointOptions
                {
                    TenantId = tenantId,
                    Url = "https://example.test/webhooks/other-event",
                    Secret = "secret-5",
                    Events = ["job.started"],
                    IsActive = true
                }
            ]
        };

        var repository = new ConfigurationWebhookRepository(new StaticOptionsMonitor<WebhookOptions>(options));

        var endpoints = await repository.GetActiveForTenantAsync(tenantId, "job.completed", CancellationToken.None);

        var endpoint = Assert.Single(endpoints);
        Assert.Equal(validEndpointId, endpoint.Id);
        Assert.Equal(tenantId, endpoint.TenantId);
        Assert.Equal("https://example.test/webhooks/jobs", endpoint.Url);
        Assert.Equal("secret-1", endpoint.Secret);
    }

    private sealed class StaticOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = currentValue;

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}