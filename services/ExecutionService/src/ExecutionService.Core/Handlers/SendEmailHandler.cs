namespace ExecutionService.Core.Handlers;

// Clients plug in their own job types by implementing IJobHandler.
// Register in DI: services.AddScoped<IJobHandler, SendEmailHandler>()
public sealed class SendEmailHandler(ILogger<SendEmailHandler> _log) : IJobHandler
{
    public string JobType => "send-email";

    public async Task<string?> HandleAsync(string payload, CancellationToken ct)
    {
        var req = JsonSerializer.Deserialize<SendEmailPayload>(payload)
            ?? throw new ArgumentException("Invalid payload");

        _log.LogInformation("Sending email to {To}", req.To);

        // Simulate email send — replace with real SMTP/SendGrid in production
        await Task.Delay(200, ct);

        return $"Email sent to {req.To} with subject '{req.Subject}'";
    }
}

public sealed record SendEmailPayload(string To, string Subject, string Body);