using ExecutionService.Core.Models;

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
        if (string.IsNullOrWhiteSpace(jobType))
            return ExecutionResult.Fail("Job type is required");

        if (!_handlers.TryGetValue(jobType, out var handler))
            return ExecutionResult.Fail(
                $"No handler registered for job type '{jobType}'");

        // Enforce 5-minute execution timeout via linked CancellationToken
        using var timeoutCts = CancellationTokenSource
            .CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(5));

        try
        {
            var result = await handler.HandleAsync(payload, timeoutCts.Token);
            return ExecutionResult.Ok(result);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested && timeoutCts.IsCancellationRequested)
        {
            return ExecutionResult.Fail("Job timed out after 5 minutes");
        }
        catch (OperationCanceledException)
        {
            return ExecutionResult.Fail("Job execution was canceled");
        }
        catch (Exception ex)
        {
            return ExecutionResult.Fail(ex.Message);
        }
    }
}
