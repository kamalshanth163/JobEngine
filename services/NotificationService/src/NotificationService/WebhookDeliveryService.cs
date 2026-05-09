using System.Security.Cryptography;
using System.Text;

namespace NotificationService.Webhooks;

public sealed class WebhookDeliveryService(
    IHttpClientFactory _http,
    IWebhookRepository _webhooks,
    ILogger<WebhookDeliveryService> _logger)
{
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
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        // Standard webhook headers
        request.Headers.Add("X-JobEngine-Event", eventType);
        request.Headers.Add("X-JobEngine-Signature", $"sha256={signature}");
        request.Headers.Add("X-JobEngine-Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

        // Retry up to 3 times: immediate, 30s, 5min
        var delays = new[] { TimeSpan.Zero, TimeSpan.FromSeconds(30),
                              TimeSpan.FromMinutes(5) };
        foreach (var delay in delays)
        {
            if (delay > TimeSpan.Zero) await Task.Delay(delay, ct);
            try
            {
                var response = await client.SendAsync(request, ct);
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