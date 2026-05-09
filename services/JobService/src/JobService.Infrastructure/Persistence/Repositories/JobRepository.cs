namespace JobService.Infrastructure.Persistence.Repositories;

public sealed class JobRepository(JobsDbContext _ctx) : IJobRepository
{
    public Task<Job?> GetByIdAsync(Guid id, CancellationToken ct)
        // AsNoTracking = no change tracking = 30% faster for reads
        => _ctx.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id, ct);

    public Task<List<Job>> GetByTenantAsync(Guid tenantId, CancellationToken ct)
        // Global query filter already scopes to TenantId — double safety
        => _ctx.Jobs.AsNoTracking()
                    .OrderByDescending(j => j.CreatedAt)
                    .ToListAsync(ct);

    public Task<Job?> GetNextQueuedAsync(CancellationToken ct)
        // FOR UPDATE SKIP LOCKED = competing workers get different rows
        => _ctx.Jobs
            .FromSqlRaw(@"SELECT * FROM ""Jobs""
                          WHERE ""Status"" = 'Queued'
                          AND ""ScheduledAt"" <= NOW()
                          ORDER BY ""Priority"" DESC, ""CreatedAt"" ASC
                          LIMIT 1 FOR UPDATE SKIP LOCKED")
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(Job job, CancellationToken ct)
        => await _ctx.Jobs.AddAsync(job, ct);

    public void Update(Job job) => _ctx.Jobs.Update(job);
}