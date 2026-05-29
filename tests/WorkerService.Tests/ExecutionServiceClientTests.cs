using System.Net;
using System.Net.Http;
using System.Text;
using WorkerService.Clients;

namespace WorkerService.Tests;

public sealed class ExecutionServiceClientTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsParsedExecutionResultOnSuccess()
    {
        HttpRequestMessage? capturedRequest = null;
        var client = new ExecutionServiceClient(new HttpClient(new DelegateHttpMessageHandler(request =>
        {
            capturedRequest = request;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\":true,\"output\":\"done\",\"error\":null}", Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }))
        {
            BaseAddress = new Uri("https://execution.test")
        });

        var result = await client.ExecuteAsync(
            new ExecuteJobRequest(Guid.NewGuid(), "send-email", "{}"),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("done", result.Output);
        Assert.Null(result.Error);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("https://execution.test/api/v1/execute", capturedRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsErrorPayloadForHttpFailure()
    {
        var client = new ExecutionServiceClient(new HttpClient(new DelegateHttpMessageHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent("upstream unavailable", Encoding.UTF8, "text/plain")
            };

            return Task.FromResult(response);
        }))
        {
            BaseAddress = new Uri("https://execution.test")
        });

        var result = await client.ExecuteAsync(
            new ExecuteJobRequest(Guid.NewGuid(), "send-email", "{}"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("HTTP 502: upstream unavailable", result.Error);
    }

    private sealed class DelegateHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request);
    }
}