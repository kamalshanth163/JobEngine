using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence;

public class AuthDbContext(DbContextOptions<AuthDbContext> options)
    : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Discovers all IEntityTypeConfiguration<T> in this assembly
        mb.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Collect domain events before saving
        var events = ChangeTracker.Entries<BaseEntity>()
            .SelectMany(e => e.Entity.DomainEvents).ToList();

        var result = await base.SaveChangesAsync(ct);

        // Dispatch AFTER save — events only fire if DB write succeeded
        foreach (var e in events) e.GetType(); // wire mediator.Publish in DI
        return result;
    }
}