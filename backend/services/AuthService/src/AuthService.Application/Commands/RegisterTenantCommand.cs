using MediatR;
using FluentValidation;
using AuthService.Application.Common.Interfaces;
using JobEngine.Shared.Common;
using AuthService.Domain.Entities;

namespace AuthService.Application.Commands;

// Command = intent to change state. Never returns domain entities — only DTOs/IDs.
public sealed record RegisterTenantCommand(
    string TenantName,
    string Slug,
    string AdminEmail,
    string AdminPassword
) : IRequest<RegisterTenantResult>;

public sealed record RegisterTenantResult(
    Guid TenantId,
    string Slug,
    string AccessToken  // JWT for immediate login after registration
);

// Validation — runs as a MediatR pipeline behaviour before the handler
public sealed class RegisterTenantValidator : AbstractValidator<RegisterTenantCommand>
{
    public RegisterTenantValidator()
    {
        RuleFor(x => x.TenantName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug)
            .NotEmpty().MaximumLength(100)
            .Matches(@"^[a-z0-9-]+$")  // URL-safe slug
            .WithMessage("Slug must be lowercase alphanumeric with hyphens only");
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.AdminPassword).NotEmpty().MinimumLength(8);
    }
}

// Handler — orchestrates domain objects and infra services. No business logic here.
public sealed class RegisterTenantHandler(
    ITenantRepository _tenants,
    IUserRepository _users,
    IPasswordHasher _hasher,
    IJwtTokenService _jwt,
    IUnitOfWork _uow
) : IRequestHandler<RegisterTenantCommand, RegisterTenantResult>
{
    public async Task<RegisterTenantResult> Handle(
        RegisterTenantCommand cmd, CancellationToken ct)
    {
        // 1. Guard against duplicate slug — business rule
        if (await _tenants.ExistsBySlugAsync(cmd.Slug, ct))
            throw new ConflictException($"Slug '{cmd.Slug}' is already taken");

        // 2. Create tenant aggregate via factory method
        var tenant = Tenant.Create(cmd.TenantName, cmd.Slug);
        await _tenants.AddAsync(tenant, ct);

        // 3. Create admin user for this tenant
        var user = User.Create(
            cmd.AdminEmail,
            _hasher.Hash(cmd.AdminPassword),
            tenant.Id,
            role: "admin");
        await _users.AddAsync(user, ct);

        // 4. Persist both in single transaction
        await _uow.SaveChangesAsync(ct);

        // 5. Issue immediate JWT so client is logged in after registration
        var (access, refresh, expiry) = _jwt.GenerateToken(user, tenant);

        return new RegisterTenantResult(tenant.Id, tenant.Slug, access);
    }
}