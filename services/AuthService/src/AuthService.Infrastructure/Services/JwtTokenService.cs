using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace AuthService.Infrastructure.Services;

public sealed class JwtTokenService(IOptions<JwtOptions> _opts) : IJwtTokenService
{
    public (string access, string refresh, DateTime expiry)
        GenerateToken(User user, Tenant tenant)
    {
        var key     = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(_opts.Value.Secret));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry  = DateTime.UtcNow.AddMinutes(_opts.Value.ExpiryMinutes);

        var claims = new []
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim(ClaimTypes.Role,           user.Role),
            // Custom claims — available in downstream services via X-Tenant-Id header
            new Claim("tenant_id",   tenant.Id.ToString()),
            new Claim("tenant_slug", tenant.Slug),
        };

        var token = new JwtSecurityToken(
            issuer:             _opts.Value.Issuer,
            audience:           _opts.Value.Audience,
            claims:             claims,
            expires:            expiry,
            signingCredentials: creds);

        var access  = new JwtSecurityTokenHandler().WriteToken(token);
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return (access, refresh, expiry);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        try
        {
            return handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_opts.Value.Secret)),
                ValidateIssuer   = true, ValidIssuer   = _opts.Value.Issuer,
                ValidateAudience = true, ValidAudience = _opts.Value.Audience,
                ClockSkew        = TimeSpan.FromSeconds(30)
            }, out _);
        }
        catch { return null; }
    }
}

public sealed class JwtOptions
{
    public string Secret        { get; set; } = default!;
    public string Issuer        { get; set; } = default!;
    public string Audience      { get; set; } = default!;
    public int    ExpiryMinutes { get; set; } = 60;
}