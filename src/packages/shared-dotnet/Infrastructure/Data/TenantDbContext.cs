using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Domain.Entities.Tenant;

namespace TabFlow.Shared.Infrastructure.Data;

public sealed class TenantDbContext(DbContextOptions<TenantDbContext> options)
    : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<TenantUserPreference> TenantUserPreferences => Set<TenantUserPreference>();
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<TableEntity> Tables => Set<TableEntity>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<QrToken> QrTokens => Set<QrToken>();
    public DbSet<CustomerSession> CustomerSessions => Set<CustomerSession>();
    public DbSet<CustomerAccessTicket> CustomerAccessTickets => Set<CustomerAccessTicket>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<TenantAuditEntry> AuditLog => Set<TenantAuditEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Station>(e =>
        {
            e.ToTable("stations");
            e.HasKey(s => s.Id);
            e.Property(s => s.Name).HasMaxLength(100).IsRequired();
            e.Property(s => s.Code).HasMaxLength(50).IsRequired();
            e.HasIndex(s => s.Code).IsUnique();
            e.Property(s => s.Color).HasMaxLength(20).IsRequired();
            e.Property(s => s.Type).HasMaxLength(50).IsRequired();
        });

        builder.Entity<TenantUserPreference>(e =>
        {
            e.ToTable("tenant_user_preferences");
            e.HasKey(p => p.UserId);
            e.Property(p => p.LanguageCode).HasMaxLength(10).IsRequired();
            e.Property(p => p.TimeZone).HasMaxLength(100).IsRequired();
            e.Property(p => p.Density).HasMaxLength(20).IsRequired();
        });

        builder.Entity<TableEntity>(e =>
        {
            e.ToTable("tables");
            e.HasKey(t => t.Id);
            e.Property(t => t.Label).HasMaxLength(50).IsRequired();
            e.HasIndex(t => t.Label).IsUnique();
            e.OwnsOne(t => t.DeviceKey, dk =>
            {
                dk.ToTable("device_keys");
                dk.WithOwner().HasForeignKey(d => d.TableId);
                dk.HasKey(d => d.TableId);
                dk.Property(d => d.DeviceKeyHash).HasMaxLength(200).IsRequired();
            });
        });

        builder.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.HasMany(c => c.Items).WithOne().HasForeignKey(i => i.CategoryId);
        });

        builder.Entity<MenuItem>(e =>
        {
            e.ToTable("menu_items");
            e.HasKey(i => i.Id);
            e.Property(i => i.Name).HasMaxLength(200).IsRequired();
            e.Property(i => i.Description).HasMaxLength(1000);
            e.Property(i => i.Price).HasColumnType("numeric(10,2)").IsRequired();
        });

        builder.Entity<QrToken>(e =>
        {
            e.ToTable("qr_tokens");
            e.HasKey(q => q.Id);
            e.Property(q => q.Value).HasMaxLength(200).IsRequired();
            e.HasIndex(q => q.Value).IsUnique();
            e.HasIndex(q => new { q.TableId, q.IsConsumed });
        });

        builder.Entity<CustomerSession>(e =>
        {
            e.ToTable("customer_sessions");
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.TableId, s.IsOpen });
            e.HasMany(s => s.AccessTickets).WithOne().HasForeignKey(t => t.SessionId);
            e.HasMany(s => s.CartItems).WithOne().HasForeignKey(c => c.SessionId);
        });

        builder.Entity<CustomerAccessTicket>(e =>
        {
            e.ToTable("customer_access_tickets");
            e.HasKey(t => t.Id);
        });

        builder.Entity<CartItem>(e =>
        {
            e.ToTable("customer_session_cart_items");
            e.HasKey(c => c.Id);
        });

        builder.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasKey(o => o.Id);
            e.Property(o => o.TotalAmount).HasColumnType("numeric(10,2)");
            e.HasIndex(o => o.TableId);
            e.HasIndex(o => o.SessionId);
            e.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId);
        });

        builder.Entity<OrderItem>(e =>
        {
            e.ToTable("order_items");
            e.HasKey(i => i.Id);
            e.Property(i => i.ItemName).HasMaxLength(200).IsRequired();
            e.Property(i => i.UnitPrice).HasColumnType("numeric(10,2)");
            e.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(i => new { i.StationId, i.Status });
        });

        builder.Entity<Bill>(e =>
        {
            e.ToTable("bills");
            e.HasKey(b => b.Id);
            e.Property(b => b.TotalAmount).HasColumnType("numeric(10,2)");
            e.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(b => b.PaymentMethod).HasMaxLength(50);
            e.HasIndex(b => new { b.TableId, b.Status });
        });

        builder.Entity<TenantAuditEntry>(e =>
        {
            e.ToTable("tenant_audit_log");
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

        // TD-0007 step 1: lift [DataClass] attributes on every
        // entity property into a `DataClass: <Classification>`
        // column comment. Migration scaffolding emits a
        // `COMMENT ON COLUMN` for each comment, so the
        // classification reaches the live database schema.
        builder.ApplyDataClassComments();
    }
}
