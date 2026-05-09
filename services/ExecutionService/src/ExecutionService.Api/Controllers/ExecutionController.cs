using System.Diagnostics;
using ExecutionService.Core.Handlers;
using ExecutionService.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ExecutionService.Api.Controllers;

[ApiController, Route("api/v1")]
public sealed class ExecutionController(JobHandlerRegistry _registry) : ControllerBase
{
    [HttpPost("execute")]
    public async Task<IActionResult> Execute(
        [FromBody] ExecuteRequest req, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var result = await _registry.ExecuteAsync(req.JobType, req.Payload, ct);
        sw.Stop();

        return Ok(ExecutionResult.Ok(result, sw.Elapsed));
    }
}

public record ExecuteRequest(Guid JobId, string JobType, string Payload);