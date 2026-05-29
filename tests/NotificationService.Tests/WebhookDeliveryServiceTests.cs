using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NotificationService.Webhooks;

namespace NotificationService.Tests;

public sealed class WebhookDeliveryServiceTests
{
    [Fact]
    public async Task DeliverAsync_SendsSignedJsonPayloadWithExpectedHeaders()
    {
        var tenantId = Guid.NewGuid();
        var endpoint = new WebhookEndpoint(
            Guid.NewGuid(),
            tenantId,
            "https://example.test/webhooks/jobs",
            "super-secret-demo",
            ["job.completed"]);
        CapturedRequest? captured = null;
        var handler = new DelegateHttpMessageHandler(async request =>
        {
            captured = new CapturedRequest(
                request.Method,
                request.RequestUri,
                await request.Content!.ReadAsStringAsync(),
                request.Headers.ToDictionary(x => x.Key, x => x.Value.ToArray()),
                request.Content!.Headers.ContentType?.MediaType);

            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var service = new WebhookDeliveryService(
            new StubHttpClientFactory(new HttpClient(handler)),
            new StubWebhookRepository(endpoint),
            NullLogger<WebhookDeliveryService>.Instance,
            [TimeSpan.Zero],
            () => 1_717_000_000);
        var payload = new { JobId = Guid.NewGuid(), Status = "completed" };

        await service.DeliverAsync(tenantId, "job.completed", payload, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("https://example.test/webhooks/jobs", captured.RequestUri!.ToString());
        Assert.Equal("application/json", captured.ContentType);

        var expectedBody = JsonSerializer.Serialize(payload);
        Assert.Equal(expectedBody, captured.Body);
        Assert.Equal(["job.completed"], captured.Headers["X-JobEngine-Event"]);
        Assert.Equal(["1717000000"], captured.Headers["X-JobEngine-Timestamp"]);
        Assert.Equal(
            [$"sha256={ComputeSignature(expectedBody, endpoint.Secret)}"],
            captured.Headers["X-JobEngine-Signature"]);
    }

    [Fact]
    public async Task DeliverAsync_RetriesUntilSuccess()
    {
        var attempts = 0;
        var tenantId = Guid.NewGuid();
        var service = new WebhookDeliveryService(
            new StubHttpClientFactory(new HttpClient(new DelegateHttpMessageHandler(_ =>
            {
                attempts++;
                var statusCode = attempts == 1 ? HttpStatusCode.InternalServerError : HttpStatusCode.OK;
                return Task.FromResult(new HttpResponseMessage(statusCode));
            }))),
            new StubWebhookRepository(new WebhookEndpoint(
                Guid.NewGuid(),
                tenantId,
                "https://example.test/webhooks/retry",
                "retry-secret",
                ["job.completed"])),
            NullLogger<WebhookDeliveryService>.Instance,
            [TimeSpan.Zero, TimeSpan.Zero],
            () => 1_717_000_001);

        await service.DeliverAsync(tenantId, "job.completed", new { Value = 42 }, CancellationToken.None);

        Assert.Equal(2, attempts);
    }

    private static string ComputeSignature(string body, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(body);
        return Convert.ToHexString(HMACSHA256.HashData(key, data)).ToLowerInvariant();
    }

    private sealed record CapturedRequest(
        HttpMethod Method,
        Uri? RequestUri,
        string Body,
        Dictionary<string, string[]> Headers,
        string? ContentType);

    private sealed class StubWebhookRepository(params WebhookEndpoint[] endpoints) : IWebhookRepository
    {
        public Task<IReadOnlyList<WebhookEndpoint>> GetActiveForTenantAsync(
            Guid tenantId,
            string eventType,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            IReadOnlyList<WebhookEndpoint> matchingEndpoints = endpoints
                .Where(x => x.TenantId == tenantId)
                .Where(x => x.IsActive)
                .Where(x => x.Events.Any(e => string.Equals(e, eventType, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            return Task.FromResult(matchingEndpoints);
        }
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class DelegateHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request);
    }
}