namespace JobService.Application.Common.Interfaces;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Job>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Job job, CancellationToken ct = default);
    Task<Job?> GetNextQueuedAsync(CancellationToken ct = default);
    void Update(Job job);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface ITenantContext
{
    Guid TenantId { get; }
    string TenantSlug { get; }
}