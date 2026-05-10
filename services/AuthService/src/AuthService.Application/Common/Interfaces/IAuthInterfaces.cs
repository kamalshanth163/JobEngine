using System.Security.Claims;
using AuthService.Domain.Entities;

namespace AuthService.Application.Common.Interfaces;

// These interfaces live in Application layer.
// Implementations live in Infrastructure.
// Application never references EF Core, Redis, or JWT libraries directly.

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct = default);
    Task AddAsync(Tenant tenant, CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<User?> GetByEmailAndTenantAsync(string email, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task<User?> GetAdminByTenantAsync(Guid tenantId, CancellationToken ct = default);
}

public interface IApiKeyRepository
{
    Task<ApiKey?> GetByHashAsync(string keyHash, CancellationToken ct = default);
    Task<List<ApiKey>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(ApiKey key, CancellationToken ct = default);
}

public interface IJwtTokenService
{
    (string access, string refresh, DateTime expiry)
        GenerateToken(User user, Tenant tenant);
    ClaimsPrincipal? ValidateToken(string token);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}