namespace AuthService.Domain.Entities;

public sealed class User : BaseEntity
{
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public Guid TenantId { get; private set; }
    public string Role { get; private set; } = "member";
    public bool IsActive { get; private set; } = true;

    private User() { }

    public static User Create(string email, string passwordHash,
        Guid tenantId, string role = "member")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return new User
        {
            Email = email.ToLowerInvariant().Trim(),
            PasswordHash = passwordHash,
            TenantId = tenantId,
            Role = role
        };
    }

    public void UpdatePassword(string newHash) => PasswordHash = newHash;
    public void Deactivate() => IsActive = false;
}