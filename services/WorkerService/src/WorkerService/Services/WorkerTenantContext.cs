using JobService.Application.Common.Interfaces;

namespace WorkerService.Services;

public sealed class WorkerTenantContext : ITenantContext
{
    public Guid TenantId => Guid.Empty;
    public string TenantSlug => "worker";
}