using System.Security.Cryptography;
using System.Text;

namespace AuthService.Domain.Entities;

public sealed class ApiKey
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string KeyHash { get; private set; } = default!;
    // Prefix shown in UI ("je_abc1...") — safe to store, not the full key
    public string KeyPrefix { get; private set; } = default!;
    public string? Name { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ApiKey() { }

    // Returns the raw key to show to the user ONCE — never store it
    public static (ApiKey entity, string rawKey) Create(
        Guid tenantId, string? name = null)
    {
        // Generate a cryptographically random 32-byte key
        var raw = "je_" + Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        // SHA-256 hash — only this goes in the database
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

        return (new ApiKey
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            KeyHash = hash,
            KeyPrefix = raw[..10],  // "je_AbCdEf..." safe to show
            Name = name,
            CreatedAt = DateTime.UtcNow
        }, raw);
    }

    // Verify a presented key against the stored hash
    public bool Verify(string rawKey)
    {
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));
        // CryptographicOperations.FixedTimeEquals prevents timing attacks
        return !IsRevoked &&
               CryptographicOperations.FixedTimeEquals(
                   Encoding.UTF8.GetBytes(KeyHash),
                   Encoding.UTF8.GetBytes(hash));
    }

    public void Revoke() => IsRevoked = true;
    public void RecordUsage() => LastUsedAt = DateTime.UtcNow;
}