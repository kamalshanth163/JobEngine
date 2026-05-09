namespace ExecutionService.Core.Handlers;

// Any new job type just implements this one interface
public interface IJobHandler
{
    string JobType { get; }
    Task<string?> HandleAsync(string payload, CancellationToken ct);
}

// Registry pattern — all handlers injected via DI, resolved by type string
public sealed class JobHandlerRegistry
{
    private readonly Dictionary<string, IJobHandler> _handlers;

    public JobHandlerRegistry(IEnumerable<IJobHandler> handlers)
        => _handlers = handlers.ToDictionary(h => h.JobType,
            StringComparer.OrdinalIgnoreCase);

    public async Task<ExecutionResult> ExecuteAsync(
        string jobType, string payload, CancellationToken ct)
    {
        if (!_handlers.TryGetValue(jobType, out var handler))
            return ExecutionResult.Failure(
                $"No handler registered for job type '{jobType}'");

        // Enforce 5-minute execution timeout via linked CancellationToken
        using var timeoutCts = CancellationTokenSource
            .CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(5));

        try
        {
            var result = await handler.HandleAsync(payload, timeoutCts.Token);
            return ExecutionResult.Success(result);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return ExecutionResult.Failure("Job timed out after 5 minutes");
        }
        catch (Exception ex)
        {
            return ExecutionResult.Failure(ex.Message);
        }
    }
}

// Example handler — clients register handlers like this
public sealed class SendEmailHandler(IEmailService _email) : IJobHandler
{
    public string JobType => "send-email";

    public async Task<string?> HandleAsync(string payload, CancellationToken ct)
    {
        var req = JsonSerializer.Deserialize<SendEmailPayload>(payload)!;
        await _email.SendAsync(req.To, req.Subject, req.Body, ct);
        return $"Email sent to {req.To}";
    }
}