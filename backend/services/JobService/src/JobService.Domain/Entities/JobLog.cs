namespace JobService.Domain.Entities;

public sealed class JobLog
{
    public long Id { get; private set; }
    public Guid JobId { get; private set; }
    public string Level { get; private set; } = "Info";
    public string Message { get; private set; } = default!;
    public DateTime LoggedAt { get; private set; }

    private JobLog() { }

    public static JobLog Create(Guid jobId, string message,
        string level = "Info") => new()
        {
            JobId = jobId,
            Message = message,
            Level = level,
            LoggedAt = DateTime.UtcNow
        };
}