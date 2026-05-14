using JobService.Application.Commands;
using JobService.Application.Common.Interfaces;
using JobService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace JobService.Api.Controllers;

[ApiController, Route("api/v1/jobs")]
public sealed class JobsController(IMediator _mediator, ITenantContext _tenant)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitJobRequest req, CancellationToken ct)
    {
        var id = await _mediator.Send(new SubmitJobCommand(
            _tenant.TenantId, req.Type, req.Payload,
            req.Priority, req.MaxAttempts, req.ScheduledAt), ct);
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var job = await _mediator.Send(
            new GetJobQuery(id, _tenant.TenantId), ct);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var jobs = await _mediator.Send(
            new ListJobsQuery(_tenant.TenantId), ct);
        return Ok(jobs);
    }
}

public record SubmitJobRequest(
    string Type,
    string Payload = "{}",
    int Priority = 0,
    int MaxAttempts = 3,
    DateTime? ScheduledAt = null);