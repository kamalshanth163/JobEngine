using MediatR;
using AuthService.Application.Common.Interfaces;

namespace AuthService.Application.Queries;

public sealed record GetTenantQuery(Guid TenantId) : IRequest<GetTenantResult>;

public sealed record GetTenantResult(Guid Id, string Slug, string Name, string AdminEmail);

public sealed class GetTenantHandler(ITenantRepository _tenants, IUserRepository _users)
    : IRequestHandler<GetTenantQuery, GetTenantResult>
{
    public async Task<GetTenantResult> Handle(GetTenantQuery req, CancellationToken ct)
    {
        var tenant = await _tenants.GetByIdAsync(req.TenantId, ct)
            ?? throw new KeyNotFoundException("Tenant not found");

        var admin = await _users.GetAdminByTenantAsync(tenant.Id, ct);
        var adminEmail = admin?.Email ?? string.Empty;

        return new GetTenantResult(tenant.Id, tenant.Slug, tenant.Name, adminEmail);
    }
}
