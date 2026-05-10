using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence;

public sealed class TenantRepository : ITenantRepository
{
    private readonly AuthDbContext _db;
    public TenantRepository(AuthDbContext db) => _db = db;

    public async Task AddAsync(Tenant tenant, CancellationToken ct = default)
    {
        await _db.Tenants.AddAsync(tenant, ct);
    }

    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await _db.Tenants.AnyAsync(t => t.Slug == slug, ct);
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var ent = await _db.Tenants.FindAsync(new object[] { id }, ct);
        return ent is null ? null : ent;
    }

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await _db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug, ct);
    }
}
