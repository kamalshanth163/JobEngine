namespace AuthService.Domain.Entities;

// Sealed = cannot be inherited. Private setters = only domain methods mutate state.
// No [Required] or [MaxLength] annotations — those belong in EF config, not domain.
public sealed class Tenant
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string Plan { get; private set; } = "free";
    public int JobQuota { get; private set; } = 1000;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    // EF Core needs a parameterless constructor — keep it private
    private Tenant() { }

    // Factory method — validates before creation, raises domain event
    public static Tenant Create(string name, string slug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        return new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Slug = slug.ToLowerInvariant().Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Upgrade(string plan, int newQuota)
    {
        Plan = plan;
        JobQuota = newQuota;
    }

    public void Deactivate() => IsActive = false;
}