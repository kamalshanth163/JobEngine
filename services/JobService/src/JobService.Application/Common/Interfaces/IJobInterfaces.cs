using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JobService.Domain.Entities;

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

public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : class;
}

public interface ITenantQuotaService
{
    Task EnforceAsync(Guid tenantId, CancellationToken ct = default);
}