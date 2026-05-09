using System.Net.Http.Json;

namespace WorkerService.Clients;

public sealed class ExecutionServiceClient(HttpClient _http) : IExecutionServiceClient
{
    public async Task<ExecutionResult> ExecuteAsync(
        ExecuteJobRequest request, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync("/api/v1/execute", request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            return ExecutionResult.Failure($"HTTP {(int)response.StatusCode}: {error}");
        }

        return await response.Content
            .ReadFromJsonAsync<ExecutionResult>(ct) ?? ExecutionResult.Failure("No response");
    }
}

public interface IExecutionServiceClient
{
    Task<ExecutionResult> ExecuteAsync(ExecuteJobRequest request, CancellationToken ct);
}

public sealed record ExecuteJobRequest(Guid JobId, string JobType, string Payload);

public sealed record ExecutionResult(bool Success, string? Output, string? Error)
{
    public static ExecutionResult Success(string? output) => new(true, output, null);
    public static ExecutionResult Failure(string error) => new(false, null, error);
}