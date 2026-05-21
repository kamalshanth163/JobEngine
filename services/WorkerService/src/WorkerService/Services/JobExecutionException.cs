namespace WorkerService.Services;

public sealed class JobExecutionException(string message) : Exception(message);