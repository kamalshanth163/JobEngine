using JobService.Application.Common.Interfaces;

namespace JobService.Api.Middleware;

// Runs before every request.
// Extracts TenantId from JWT claim or X-API-Key header.
// Injects into ITenantContext — available to all controllers and handlers.
public sealed class TenantContextMiddleware(RequestDelegate _next)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        // From JWT: "tenant_id" claim injected by Auth Service
        var tenantClaim = ctx.User.FindFirst("tenant_id")?.Value;

        // From API key: gateway forwards X-Tenant-Id header after validation
        var tenantHeader = ctx.Request.Headers["X-Tenant-Id"].FirstOrDefault();

        var raw = tenantClaim ?? tenantHeader;
        if (Guid.TryParse(raw, out var tenantId))
        {
            var tc = ctx.RequestServices.GetRequiredService<ITenantContext>();
            ((HttpTenantContext)tc).Set(tenantId, "");
        }

        await _next(ctx);
    }
}

public sealed class HttpTenantContext : ITenantContext
{
    public Guid TenantId { get; private set; }
    public string TenantSlug { get; private set; } = "";
    public void Set(Guid id, string slug)
    {
        TenantId = id;
        TenantSlug = slug;
    }
}