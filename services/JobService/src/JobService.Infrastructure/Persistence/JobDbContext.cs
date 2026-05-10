using JobService.Application.Common.Interfaces;
using JobService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobService.Infrastructure.Persistence;

public class JobsDbContext(DbContextOptions<JobsDbContext> options,
    ITenantContext tenantContext) : DbContext(options)
{
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobLog> JobLogs => Set<JobLog>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Auto-discover all IEntityTypeConfiguration in this assembly
        mb.ApplyConfigurationsFromAssembly(typeof(JobsDbContext).Assembly);

        // CRITICAL: Global query filter — every query auto-scoped to tenant
        // A developer cannot accidentally query across tenant boundaries
        // To bypass (admin only): use .IgnoreQueryFilters()
        mb.Entity<Job>()
          .HasQueryFilter(j => j.TenantId == tenantContext.TenantId);
    }
}

// EF Core Fluent configuration — zero attributes on domain entities
public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> b)
    {
        b.HasKey(j => j.Id);
        b.Property(j => j.Type).HasMaxLength(100).IsRequired();
        b.Property(j => j.Payload).HasColumnType("jsonb"); // native JSON in PG
        b.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);

        // Composite index — critical for worker polling performance
        // WHERE status = 'Queued' AND scheduled_at <= now ORDER BY priority DESC
        b.HasIndex(j => new { j.TenantId, j.Status, j.ScheduledAt })
         .HasDatabaseName("idx_jobs_tenant_status_scheduled");

        b.HasMany(j => j.Logs)
         .WithOne()
         .HasForeignKey("JobId")
         .OnDelete(DeleteBehavior.Cascade);
    }
}