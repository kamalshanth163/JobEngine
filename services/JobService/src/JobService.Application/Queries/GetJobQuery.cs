using MediatR;
using JobService.Application.Common.Interfaces;
using JobService.Domain.Entities;
using System;

namespace JobService.Application.Queries;

public sealed record GetJobQuery(Guid JobId, Guid TenantId)
    : IRequest<JobDto?>;

public sealed class GetJobHandler(
    IJobRepository _jobs
) : IRequestHandler<GetJobQuery, JobDto?>
{
    public async Task<JobDto?> Handle(
        GetJobQuery q, CancellationToken ct)
    {
        // AsNoTracking = no EF change tracking = faster reads
        // Global query filter ensures only this tenant's job is returned
        var job = await _jobs.GetByIdAsync(q.JobId, ct);
        return job is null ? null : JobDto.FromDomain(job);
    }
}

public sealed record JobDto(
    Guid Id, Guid TenantId, string Type,
    string Status, int Attempt, int MaxAttempts,
    int Priority, string? Error, string? Result,
    DateTime CreatedAt, DateTime? StartedAt, DateTime? CompletedAt
)
{
    public static JobDto FromDomain(Job j) => new(
        j.Id, j.TenantId, j.Type,
        j.Status.ToString(), j.Attempt, j.MaxAttempts,
        j.Priority, j.Error, j.Result,
        j.CreatedAt, j.StartedAt, j.CompletedAt);
}