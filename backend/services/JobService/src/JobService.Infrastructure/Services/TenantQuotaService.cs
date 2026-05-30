using JobService.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace JobService.Infrastructure.Services;

public sealed class TenantQuotaService : ITenantQuotaService
{
    private readonly IJobRepository _jobs;
    private readonly IConfiguration _config;

    public TenantQuotaService(IJobRepository jobs, IConfiguration config)
    {
        _jobs = jobs;
        _config = config;
    }

    public async Task EnforceAsync(Guid tenantId, CancellationToken ct = default)
    {
        // Simple quota: configurable per-app, default 100 active jobs
        var limit = int.TryParse(_config["Tenants:DefaultJobQuota"], out var v) ? v : 100;

        var list = await _jobs.GetByTenantAsync(tenantId, ct);
        // count non-terminal statuses
        var active = list.Count(
            j => j.Status == Domain.Entities.JobStatus.Pending
            || j.Status == Domain.Entities.JobStatus.Queued
            || j.Status == Domain.Entities.JobStatus.Running
            || j.Status == Domain.Entities.JobStatus.Retrying);

        if (active >= limit) throw new InvalidOperationException("Tenant quota exceeded");
    }
}
