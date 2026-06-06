using ExecutionService.Core.Handlers;

namespace ExecutionService.Tests;

public sealed class JobHandlerRegistryTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsFailureForUnknownJobType()
    {
        var registry = new JobHandlerRegistry([]);

        var result = await registry.ExecuteAsync("missing", "{}", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("No handler registered for job type 'missing'", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_UsesMatchingHandlerCaseInsensitively()
    {
        var registry = new JobHandlerRegistry([new StubJobHandler("send-email", _ => Task.FromResult<string?>("sent"))]);

        var result = await registry.ExecuteAsync("SEND-EMAIL", "{}", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("sent", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFailureWhenHandlerThrows()
    {
        var registry = new JobHandlerRegistry([new StubJobHandler("send-email", _ => throw new InvalidOperationException("boom"))]);

        var result = await registry.ExecuteAsync("send-email", "{}", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("boom", result.Error);
    }

    private sealed class StubJobHandler(string jobType, Func<string, Task<string?>> handle) : IJobHandler
    {
        public string JobType { get; } = jobType;

        public Task<string?> HandleAsync(string payload, CancellationToken ct) => handle(payload);
    }
}