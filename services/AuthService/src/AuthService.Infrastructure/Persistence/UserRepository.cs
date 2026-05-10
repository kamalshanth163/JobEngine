using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence;

public sealed class UserRepository : IUserRepository
{
    private readonly AuthDbContext _db;
    public UserRepository(AuthDbContext db) => _db = db;

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _db.Users.AddAsync(user, ct);
    }

    public async Task<User?> GetByEmailAndTenantAsync(string email, Guid tenantId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var normalized = email.Trim().ToLowerInvariant();
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalized && u.TenantId == tenantId, ct);
    }

    public async Task<User?> GetAdminByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Role == "admin", ct);
    }
}
