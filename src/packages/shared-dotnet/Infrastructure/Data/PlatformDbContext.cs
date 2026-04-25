using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Domain.Entities.Platform;

namespace TabFlow.Shared.Infrastructure.Data;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options)
    : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<TenantRegistration> Tenants => Set<TenantRegistration>();
    public DbSet<ProvisioningJob> ProvisioningJobs => Set<ProvisioningJob>();
    public DbSet<ProvisioningJobStep> ProvisioningJobSteps => Set<ProvisioningJobStep>();
    public DbSet<PlatformAuditEntry> AuditLog => Set<PlatformAuditEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<TenantRegistration>(e =>
        {
            e.ToTable("tenants");
            e.HasKey(t => t.Id);
            e.Property(t => t.Code).HasMaxLength(63).IsRequired();
            e.HasIndex(t => t.Code).IsUnique();
            e.Property(t => t.DisplayName).HasMaxLength(200).IsRequired();
            e.Property(t => t.PrimaryDomain).HasMaxLength(253).IsRequired();
            e.HasIndex(t => t.PrimaryDomain).IsUnique();
            e.Property(t => t.LanguageCode).HasMaxLength(10).IsRequired();
            e.Property(t => t.CurrencyCode).HasMaxLength(3).IsRequired();
            e.Property(t => t.TimeZone).HasMaxLength(100).IsRequired();
            e.Property(t => t.IntendedOwnerEmail).HasMaxLength(320).IsRequired();
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.DatabaseName).HasMaxLength(63);
            e.Property(t => t.DatabaseUser).HasMaxLength(63);
            e.Property(t => t.DatabasePassword).HasMaxLength(100);
        });

        builder.Entity<ProvisioningJob>(e =>
        {
            e.ToTable("provisioning_jobs");
            e.HasKey(j => j.Id);
            e.Property(j => j.Type).HasMaxLength(50).IsRequired();
            e.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(j => j.ClaimedBy).HasMaxLength(100);
            e.Property(j => j.Payload).HasColumnType("jsonb");
            e.Property(j => j.Result).HasColumnType("jsonb");
            e.HasMany(j => j.Steps).WithOne().HasForeignKey(s => s.JobId);
            e.HasIndex(j => j.Status);
            e.HasIndex(j => j.TenantId);
        });

        builder.Entity<ProvisioningJobStep>(e =>
        {
            e.ToTable("provisioning_job_steps");
            e.HasKey(s => s.Id);
            e.Property(s => s.Name).HasMaxLength(100).IsRequired();
            e.Property(s => s.Status).HasMaxLength(20).IsRequired();
        });

        builder.Entity<PlatformAuditEntry>(e =>
        {
            e.ToTable("platform_audit_log");
            e.HasKey(a => a.Id);
            e.Property(a => a.ActorEmail).HasMaxLength(320).IsRequired();
            e.Property(a => a.Action).HasMaxLength(100).IsRequired();
            e.Property(a => a.ResourceType).HasMaxLength(100).IsRequired();
            e.Property(a => a.ResourceId).HasMaxLength(200).IsRequired();
            e.Property(a => a.Changes).HasColumnType("jsonb");
            e.Property(a => a.Ip).HasMaxLength(45);
            e.Property(a => a.UserAgent).HasMaxLength(500);
            e.HasIndex(a => a.CreatedAt);
            e.HasIndex(a => a.Action);
        });
    }
}
