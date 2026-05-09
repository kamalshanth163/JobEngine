using AuthService.Application.Commands;
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
    public IActionResult GetTenant(Guid id) => Ok(new { id });
}

public record RegisterTenantRequest(
    string TenantName, string Slug,
    string AdminEmail, string AdminPassword);

public record LoginRequest(string Email, string Password, string TenantSlug);