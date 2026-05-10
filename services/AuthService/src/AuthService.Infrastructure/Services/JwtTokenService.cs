using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Entities;
using System.Text;
using System.Security.Cryptography;
using StackExchange.Redis;
using System.Text.Json;


namespace AuthService.Infrastructure.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _opts;
    private readonly IConnectionMultiplexer? _redis;

    public JwtTokenService(IOptions<JwtOptions> opts, IConnectionMultiplexer? redis = null)
    {
        _opts = opts.Value;
        _redis = redis;
    }

    public (string access, string refresh, DateTime expiry)
        GenerateToken(User user, Tenant tenant)
    {
        var key = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(_opts.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(_opts.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim(ClaimTypes.Role,           user.Role),
            // Custom claims — available in downstream services via X-Tenant-Id header
            new Claim("tenant_id",   tenant.Id.ToString()),
            new Claim("tenant_slug", tenant.Slug),
        };

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            expires: expiry,
            signingCredentials: creds);

        var access = new JwtSecurityTokenHandler().WriteToken(token);
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        // Cache token claims in Redis for faster validation
        if (_redis is not null)
        {
            try
            {
                var db = _redis.GetDatabase();
                var keyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(access)));
                var payload = JsonSerializer.Serialize(claims.Select(c => new { c.Type, c.Value }));
                var ttl = expiry - DateTime.UtcNow;
                if (ttl.TotalSeconds > 0)
                    db.StringSet($"jwt:{keyHash}", payload, ttl);
            }
            catch { }
        }

        return (access, refresh, expiry);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        try
        {
            // Check Redis cache first
            if (_redis is not null)
            {
                try
                {
                    var db = _redis.GetDatabase();
                    var keyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
                    var cached = db.StringGet($"jwt:{keyHash}");
                    if (cached.HasValue)
                    {
                        var items = JsonSerializer.Deserialize<List<ClaimDto>>(cached.ToString()!)!;
                        var claims = items.Select(i => new Claim(i.Type, i.Value)).ToArray();
                        var identity = new ClaimsIdentity(claims, "jwt");
                        return new ClaimsPrincipal(identity);
                    }
                }
                catch { }
            }

            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_opts.Secret)),
                ValidateIssuer = true,
                ValidIssuer = _opts.Issuer,
                ValidateAudience = true,
                ValidAudience = _opts.Audience,
                ClockSkew = TimeSpan.FromSeconds(30)
            }, out var validatedToken);

            // Cache validated claims for subsequent requests
            if (_redis is not null && principal is not null)
            {
                try
                {
                    var db = _redis.GetDatabase();
                    var keyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
                    var claims = principal.Claims.Select(c => new ClaimDto { Type = c.Type, Value = c.Value }).ToList();
                    var payload = JsonSerializer.Serialize(claims);
                    // Use token's ValidTo property for expiry
                    var ttl = validatedToken?.ValidTo - DateTime.UtcNow ?? TimeSpan.FromMinutes(5);
                    if (ttl.TotalSeconds > 0)
                        db.StringSet($"jwt:{keyHash}", payload, ttl);
                }
                catch { }
            }

            return principal;
        }
        catch { return null; }
    }
}

internal sealed class ClaimDto { public string Type { get; set; } = default!; public string Value { get; set; } = default!; }

public sealed class JwtOptions
{
    public string Secret { get; set; } = default!;
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public int ExpiryMinutes { get; set; } = 60;
}