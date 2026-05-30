using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NotificationService.Webhooks;

public sealed class WebhookDeliveryService
{
    private static readonly TimeSpan[] DefaultRetryDelays =
        [TimeSpan.Zero, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5)];

    private readonly IHttpClientFactory _http;
    private readonly IWebhookRepository _webhooks;
    private readonly ILogger<WebhookDeliveryService> _logger;
    private readonly IReadOnlyList<TimeSpan> _retryDelays;
    private readonly Func<long> _timestampProvider;

    public WebhookDeliveryService(
        IHttpClientFactory http,
        IWebhookRepository webhooks,
        ILogger<WebhookDeliveryService> logger)
        : this(http, webhooks, logger, DefaultRetryDelays, static () => DateTimeOffset.UtcNow.ToUnixTimeSeconds())
    {
    }

    internal WebhookDeliveryService(
        IHttpClientFactory http,
        IWebhookRepository webhooks,
        ILogger<WebhookDeliveryService> logger,
        IReadOnlyList<TimeSpan> retryDelays,
        Func<long> timestampProvider)
    {
        _http = http;
        _webhooks = webhooks;
        _logger = logger;
        _retryDelays = retryDelays.Count == 0 ? DefaultRetryDelays : retryDelays;
        _timestampProvider = timestampProvider;
    }

    public async Task DeliverAsync(
        Guid tenantId, string eventType,
        object payload, CancellationToken ct)
    {
        // Find all active webhook endpoints for this tenant + event type
        var endpoints = await _webhooks
            .GetActiveForTenantAsync(tenantId, eventType, ct);

        var body = JsonSerializer.Serialize(payload);

        foreach (var endpoint in endpoints)
        {
            await DeliverToEndpointAsync(endpoint, eventType, body, ct);
        }
    }

    private async Task DeliverToEndpointAsync(
        WebhookEndpoint endpoint, string eventType,
        string body, CancellationToken ct)
    {
        // HMAC-SHA256 signature — client verifies this to confirm authenticity
        // Same pattern used by Stripe, GitHub, Shopify webhooks
        var signature = ComputeSignature(body, endpoint.Secret);

        var client = _http.CreateClient();

        // Retry up to 3 times: immediate, 30s, 5min
        foreach (var delay in _retryDelays)
        {
            if (delay > TimeSpan.Zero) await Task.Delay(delay, ct);
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };

                // Standard webhook headers
                request.Headers.Add("X-JobEngine-Event", eventType);
                request.Headers.Add("X-JobEngine-Signature", $"sha256={signature}");
                request.Headers.Add("X-JobEngine-Timestamp", _timestampProvider().ToString());

                using var response = await client.SendAsync(request, ct);
                if (response.IsSuccessStatusCode) return; // delivered!
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Webhook delivery failed to {Url}", endpoint.Url);
            }
        }
        _logger.LogError("Webhook permanently failed for endpoint {Id}", endpoint.Id);
    }

    // Compute HMAC-SHA256 — client verifies with their stored secret
    private static string ComputeSignature(string body, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(body);
        return Convert.ToHexString(HMACSHA256.HashData(key, data)).ToLower();
    }
}