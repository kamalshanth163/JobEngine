namespace ExecutionService.Core.Models;

public sealed record ExecutionResult
{
    public bool Success { get; init; }
    public string? Output { get; init; }
    public string? Error { get; init; }
    public TimeSpan Duration { get; init; }

    public static ExecutionResult Ok(string? output, TimeSpan duration) =>
        new() { Success = true, Output = output, Duration = duration };

    public static ExecutionResult Fail(string error, TimeSpan duration) =>
        new() { Success = false, Error = error, Duration = duration };
}