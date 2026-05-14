using MediatR;
using JobService.Application.Common.Interfaces;
using JobService.Domain.Entities;

namespace JobService.Application.Queries;

public sealed record ListJobsQuery(Guid TenantId) : IRequest<IEnumerable<JobDto>>;

public sealed class ListJobsHandler(IJobRepository _jobs) : IRequestHandler<ListJobsQuery, IEnumerable<JobDto>>
{
    public async Task<IEnumerable<JobDto>> Handle(ListJobsQuery q, CancellationToken ct)
    {
        var list = await _jobs.GetByTenantAsync(q.TenantId, ct);
        return list.Select(JobDto.FromDomain);
    }
}
