namespace ExecutionService.Core.Models;

public sealed record ExecutionResult
{
    public bool Success { get; init; }
    public string? Output { get; init; }
    public string? Error { get; init; }
    public TimeSpan Duration { get; init; }

    public static ExecutionResult Ok(string? output) =>
        new() { Success = true, Output = output };

    public static ExecutionResult Fail(string error) =>
        new() { Success = false, Error = error };
}