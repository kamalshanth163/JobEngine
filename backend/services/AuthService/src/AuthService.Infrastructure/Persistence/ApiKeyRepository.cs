using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence;

public sealed class ApiKeyRepository : IApiKeyRepository
{
    private readonly AuthDbContext _db;
    public ApiKeyRepository(AuthDbContext db) => _db = db;

    public async Task AddAsync(ApiKey key, CancellationToken ct = default)
    {
        await _db.ApiKeys.AddAsync(key, ct);
    }

    public async Task<ApiKey?> GetByHashAsync(string keyHash, CancellationToken ct = default)
    {
        return await _db.ApiKeys.FirstOrDefaultAsync(k => k.KeyHash == keyHash, ct);
    }

    public async Task<List<ApiKey>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _db.ApiKeys.Where(k => k.TenantId == tenantId).ToListAsync(ct);
    }
}
