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
            return ExecutionResult.Fail($"HTTP {(int)response.StatusCode}: {error}");
        }

        return await response.Content
            .ReadFromJsonAsync<ExecutionResult>(ct) ?? ExecutionResult.Fail("No response");
    }
}

public interface IExecutionServiceClient
{
    Task<ExecutionResult> ExecuteAsync(ExecuteJobRequest request, CancellationToken ct);
}

public sealed record ExecuteJobRequest(Guid JobId, string JobType, string Payload);

public sealed record ExecutionResult(bool Success, string? Output, string? Error)
{
    public static ExecutionResult Ok(string? output) => new(true, output, null);
    public static ExecutionResult Fail(string error) => new(false, null, error);
}