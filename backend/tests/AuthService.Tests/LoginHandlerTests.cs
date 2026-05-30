using AuthService.Application.Commands;
using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Entities;
using JobEngine.Shared.Common;

namespace AuthService.Tests;

public sealed class LoginHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsTokensForValidCredentials()
    {
        var tenant = Tenant.Create("Acme", "acme");
        var user = User.Create("admin@acme.test", "hashed-password", tenant.Id, "admin");
        var expectedExpiry = new DateTime(2026, 5, 29, 12, 0, 0, DateTimeKind.Utc);
        var handler = new LoginHandler(
            new StubUserRepository(user),
            new StubTenantRepository(tenant),
            new StubPasswordHasher(verifyResult: true),
            new StubJwtTokenService(("access-token", "refresh-token", expectedExpiry)));

        var result = await handler.Handle(
            new LoginCommand(user.Email, "correct horse battery staple", tenant.Slug),
            CancellationToken.None);

        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal(expectedExpiry, result.ExpiresAt);
        Assert.Equal(tenant.Id, result.TenantId);
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorizedForInvalidPassword()
    {
        var tenant = Tenant.Create("Acme", "acme");
        var user = User.Create("admin@acme.test", "hashed-password", tenant.Id, "admin");
        var handler = new LoginHandler(
            new StubUserRepository(user),
            new StubTenantRepository(tenant),
            new StubPasswordHasher(verifyResult: false),
            new StubJwtTokenService(("access-token", "refresh-token", DateTime.UtcNow)));

        await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(
            new LoginCommand(user.Email, "wrong-password", tenant.Slug),
            CancellationToken.None));
    }

    private sealed class StubTenantRepository(Tenant? tenant) : ITenantRepository
    {
        public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(tenant?.Id == id ? tenant : null);

        public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
            Task.FromResult(string.Equals(tenant?.Slug, slug, StringComparison.OrdinalIgnoreCase) ? tenant : null);

        public Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct = default) =>
            Task.FromResult(string.Equals(tenant?.Slug, slug, StringComparison.OrdinalIgnoreCase));

        public Task AddAsync(Tenant tenant, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubUserRepository(User? user) : IUserRepository
    {
        public Task<User?> GetByEmailAndTenantAsync(string email, Guid tenantId, CancellationToken ct = default) =>
            Task.FromResult(
                user is not null
                && string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)
                && user.TenantId == tenantId
                    ? user
                    : null);

        public Task AddAsync(User user, CancellationToken ct = default) => Task.CompletedTask;

        public Task<User?> GetAdminByTenantAsync(Guid tenantId, CancellationToken ct = default) =>
            Task.FromResult(user?.TenantId == tenantId ? user : null);
    }

    private sealed class StubPasswordHasher(bool verifyResult) : IPasswordHasher
    {
        public string Hash(string password) => "hashed";

        public bool Verify(string password, string hash) => verifyResult;
    }

    private sealed class StubJwtTokenService((string access, string refresh, DateTime expiry) tokenResult) : IJwtTokenService
    {
        public (string access, string refresh, DateTime expiry) GenerateToken(User user, Tenant tenant) => tokenResult;

        public System.Security.Claims.ClaimsPrincipal? ValidateToken(string token) => null;
    }
}