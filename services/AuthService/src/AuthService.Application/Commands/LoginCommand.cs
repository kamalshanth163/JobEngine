using MediatR;
using AuthService.Application.Common.Interfaces;
using JobEngine.Shared.Common;

namespace AuthService.Application.Commands;

public sealed record LoginCommand(
    string Email,
    string Password,
    string TenantSlug
) : IRequest<LoginResult>;

public sealed record LoginResult(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    Guid TenantId
);

public sealed class LoginHandler(
    IUserRepository _users,
    ITenantRepository _tenants,
    IPasswordHasher _hasher,
    IJwtTokenService _jwt
) : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(
        LoginCommand cmd, CancellationToken ct)
    {
        var tenant = await _tenants.GetBySlugAsync(cmd.TenantSlug, ct)
            ?? throw new UnauthorizedException();

        var user = await _users.GetByEmailAndTenantAsync(
            cmd.Email, tenant.Id, ct)
            ?? throw new UnauthorizedException();

        // FixedTimeEquals prevents timing attacks on password comparison
        if (!_hasher.Verify(cmd.Password, user.PasswordHash))
            throw new UnauthorizedException();

        var (access, refresh, expiry) = _jwt.GenerateToken(user, tenant);
        return new LoginResult(access, refresh, expiry, tenant.Id);
    }
}