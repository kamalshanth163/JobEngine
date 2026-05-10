using AuthService.Application.Commands;
using AuthService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

[ApiController, Route("api/v1/auth")]
public sealed class AuthController(IMediator _mediator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterTenantRequest req,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new RegisterTenantCommand(
            req.TenantName, req.Slug, req.AdminEmail, req.AdminPassword), ct);
        return CreatedAtAction(nameof(GetTenant), new { id = result.TenantId }, result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest req,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new LoginCommand(req.Email, req.Password, req.TenantSlug), ct);
        return Ok(result);
    }

    [HttpGet("tenants/{id:guid}")]
    public async Task<IActionResult> GetTenant(Guid id)
    {
        var result = await _mediator.Send(new GetTenantQuery(id));
        return Ok(new { id = result.Id, slug = result.Slug, name = result.Name, adminEmail = result.AdminEmail });
    }

    [HttpPost("tenants/{tenantId:guid}/keys")]
    public async Task<IActionResult> CreateApiKey(Guid tenantId,
        [FromBody] CreateApiKeyRequest req,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateApiKeyCommand(tenantId, req.Name), ct);
        return CreatedAtAction(nameof(GetTenant), new { id = result.Id }, result);
    }
}

public record RegisterTenantRequest(
    string TenantName,
    string Slug,
    string AdminEmail,
    string AdminPassword
);

public record CreateApiKeyRequest(string? Name);

public record LoginRequest(string Email, string Password, string TenantSlug);