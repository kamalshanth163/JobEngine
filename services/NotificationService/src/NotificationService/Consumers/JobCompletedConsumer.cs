using MassTransit;

namespace NotificationService.Consumers;

public sealed class JobCompletedConsumer(
    WebhookDeliveryService _webhooks,
    ILogger<JobCompletedConsumer> _log
) : IConsumer<JobCompletedEvent>
{
    public async Task Consume(ConsumeContext<JobCompletedEvent> ctx)
    {
        _log.LogInformation("Delivering webhooks for job {Id}", ctx.Message.JobId);
        await _webhooks.DeliverAsync(
            ctx.Message.TenantId,
            "job.completed",
            ctx.Message,
            ctx.CancellationToken);
    }
}

public sealed class JobFailedConsumer(
    WebhookDeliveryService _webhooks,
    ILogger<JobFailedConsumer> _log
) : IConsumer<JobFailedEvent>
{
    public async Task Consume(ConsumeContext<JobFailedEvent> ctx)
    {
        if (ctx.Message.IsFinal) // only fire webhook when fully dead-lettered
        {
            await _webhooks.DeliverAsync(
                ctx.Message.TenantId,
                "job.failed",
                ctx.Message,
                ctx.CancellationToken);
        }
    }
}