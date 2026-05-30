namespace WorkerService.Services;

public interface IWorkerIdentity
{
    string WorkerId { get; }
}

public sealed class WorkerIdentity : IWorkerIdentity
{
    public string WorkerId { get; } =
        Environment.MachineName + "-" + Guid.NewGuid().ToString("N")[..6];
}
