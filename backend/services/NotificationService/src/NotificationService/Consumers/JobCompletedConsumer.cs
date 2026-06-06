using JobEngine.Shared.Contracts.Events;
using MassTransit;
using NotificationService.Webhooks;

namespace NotificationService.Consumers;

public sealed class JobCompletedConsumer(
    WebhookDeliveryService _webhooks,
    ILogger<JobCompletedConsumer> _log
) : IConsumer<JobCompletedEvent>
{
    public async Task Consume(ConsumeContext<JobCompletedEvent> ctx)
    {
        _log.LogInformation("Delivering webhooks for job {Id}", ctx.Message.JobId);

        if (!string.IsNullOrWhiteSpace(ctx.Message.WebhookUrl))
        {
            // Per-job webhook — deliver directly to the URL stored at submission time
            await _webhooks.DeliverDirectAsync(
                ctx.Message.WebhookUrl,
                ctx.Message.WebhookSecret,
                "job.completed",
                ctx.Message,
                ctx.CancellationToken);
        }
        else
        {
            // Fall back to tenant-level webhooks configured via env vars
            await _webhooks.DeliverAsync(
                ctx.Message.TenantId,
                "job.completed",
                ctx.Message,
                ctx.CancellationToken);
        }
    }
}

public sealed class JobFailedConsumer(
    WebhookDeliveryService _webhooks,
    ILogger<JobFailedConsumer> _log
) : IConsumer<JobFailedEvent>
{
    public async Task Consume(ConsumeContext<JobFailedEvent> ctx)
    {
        _log.LogInformation("Handling failed job event for job {Id}; final={IsFinal}", ctx.Message.JobId, ctx.Message.IsFinal);

        if (ctx.Message.IsFinal) // only fire webhook when fully dead-lettered
        {
            if (!string.IsNullOrWhiteSpace(ctx.Message.WebhookUrl))
            {
                await _webhooks.DeliverDirectAsync(
                    ctx.Message.WebhookUrl,
                    ctx.Message.WebhookSecret,
                    "job.failed",
                    ctx.Message,
                    ctx.CancellationToken);
            }
            else
            {
                await _webhooks.DeliverAsync(
                    ctx.Message.TenantId,
                    "job.failed",
                    ctx.Message,
                    ctx.CancellationToken);
            }
        }
    }
}