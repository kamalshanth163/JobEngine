using JobService.Application.Common.Interfaces;
using JobService.Infrastructure.Persistence;
using System.Threading;
using System.Threading.Tasks;

namespace JobService.Infrastructure.Services;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly JobsDbContext _ctx;
    public EfUnitOfWork(JobsDbContext ctx) => _ctx = ctx;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _ctx.SaveChangesAsync(ct);
}
