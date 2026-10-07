using AcxiomCRM.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AcxiomCRM.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // SQLite has no decimal type, storing as REAL lets us SUM / ORDER BY amounts in the database
        builder.Properties<decimal>().HaveConversion<double>();
        // SQLite does not keep DateTimeKind, all DateTimes are saved in UTC so mark them as UTC when reading
        builder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            e.HasOne(u => u.Manager).WithMany().HasForeignKey(u => u.ManagerId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ApplicationRole>(e => e.Property(r => r.Description).HasMaxLength(200));

        builder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.CustomerId);
            e.Property(c => c.CustomerCode).HasMaxLength(20);
            e.Property(c => c.CustomerName).HasMaxLength(100).IsRequired();
            e.Property(c => c.Email).HasMaxLength(150).IsRequired();
            e.Property(c => c.Phone).HasMaxLength(10).IsRequired();
            e.Property(c => c.CompanyName).HasMaxLength(100);
            e.Property(c => c.Address).HasMaxLength(250);
            e.Property(c => c.City).HasMaxLength(50);
            e.Property(c => c.State).HasMaxLength(50);
            e.Property(c => c.Status).HasMaxLength(20).IsRequired();
            e.Property(c => c.Notes).HasMaxLength(1000);
            // Duplicate customers are blocked by unique email and phone
            e.HasIndex(c => c.Email).IsUnique();
            e.HasIndex(c => c.Phone).IsUnique();
            e.HasIndex(c => c.CustomerCode).IsUnique();
            e.HasIndex(c => c.AssignedTo);
            e.HasIndex(c => c.Status);
            e.HasOne(c => c.AssignedUser).WithMany().HasForeignKey(c => c.AssignedTo).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Creator).WithMany().HasForeignKey(c => c.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(c => !c.IsDeleted);
        });

        builder.Entity<Lead>(e =>
        {
            e.HasKey(l => l.LeadId);
            e.Property(l => l.LeadCode).HasMaxLength(20);
            e.Property(l => l.LeadName).HasMaxLength(100).IsRequired();
            e.Property(l => l.Email).HasMaxLength(150);
            e.Property(l => l.Phone).HasMaxLength(10);
            e.Property(l => l.CompanyName).HasMaxLength(100);
            e.Property(l => l.Source).HasMaxLength(30).IsRequired();
            e.Property(l => l.Status).HasMaxLength(20).IsRequired();
            e.Property(l => l.Priority).HasMaxLength(10).IsRequired();
            e.Property(l => l.Notes).HasMaxLength(1000);
            e.HasIndex(l => l.LeadCode).IsUnique();
            e.HasIndex(l => l.AssignedTo);
            e.HasIndex(l => l.Status);
            e.HasOne(l => l.AssignedUser).WithMany().HasForeignKey(l => l.AssignedTo).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.ConvertedCustomer).WithMany().HasForeignKey(l => l.ConvertedCustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(l => !l.IsDeleted);
        });

        builder.Entity<Opportunity>(e =>
        {
            e.HasKey(o => o.OpportunityId);
            e.Property(o => o.OpportunityName).HasMaxLength(150).IsRequired();
            e.Property(o => o.Stage).HasMaxLength(20).IsRequired();
            e.Property(o => o.Status).HasMaxLength(10).IsRequired();
            e.Property(o => o.Source).HasMaxLength(30);
            e.Property(o => o.Notes).HasMaxLength(1000);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Opportunity_Amount", "Amount >= 0");
                t.HasCheckConstraint("CK_Opportunity_Probability", "Probability BETWEEN 0 AND 100");
            });
            e.HasIndex(o => o.AssignedTo);
            e.HasIndex(o => o.CustomerId);
            e.HasIndex(o => o.Stage);
            e.HasOne(o => o.Customer).WithMany(c => c.Opportunities).HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.Lead).WithMany().HasForeignKey(o => o.LeadId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.AssignedUser).WithMany().HasForeignKey(o => o.AssignedTo).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(o => !o.IsDeleted);
        });

        builder.Entity<FollowUp>(e =>
        {
            e.HasKey(f => f.FollowUpId);
            e.Property(f => f.Subject).HasMaxLength(150).IsRequired();
            e.Property(f => f.FollowUpType).HasMaxLength(20).IsRequired();
            e.Property(f => f.Status).HasMaxLength(20).IsRequired();
            e.Property(f => f.Remarks).HasMaxLength(1000);
            e.HasIndex(f => f.AssignedTo);
            e.HasIndex(f => f.FollowUpDate);
            e.HasIndex(f => f.Status);
            e.HasOne(f => f.Customer).WithMany().HasForeignKey(f => f.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(f => f.Lead).WithMany().HasForeignKey(f => f.LeadId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(f => f.Opportunity).WithMany().HasForeignKey(f => f.OpportunityId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(f => f.AssignedUser).WithMany().HasForeignKey(f => f.AssignedTo).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(f => !f.IsDeleted);
        });

        builder.Entity<Activity>(e =>
        {
            e.HasKey(a => a.ActivityId);
            e.Property(a => a.ActivityType).HasMaxLength(20).IsRequired();
            e.Property(a => a.Subject).HasMaxLength(150).IsRequired();
            e.Property(a => a.Description).HasMaxLength(1000);
            e.Property(a => a.Status).HasMaxLength(20).IsRequired();
            e.HasIndex(a => a.AssignedTo);
            e.HasIndex(a => a.ActivityDate);
            e.HasOne(a => a.Customer).WithMany().HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Lead).WithMany().HasForeignKey(a => a.LeadId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.AssignedUser).WithMany().HasForeignKey(a => a.AssignedTo).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(a => !a.IsDeleted);
        });

        builder.Entity<PasswordHistory>(e =>
        {
            e.HasKey(p => p.PasswordHistoryId);
            e.Property(p => p.PasswordHash).IsRequired();
            e.HasIndex(p => p.UserId);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.AuditLogId);
            e.Property(a => a.UserName).HasMaxLength(100);
            e.Property(a => a.Action).HasMaxLength(30).IsRequired();
            e.Property(a => a.EntityName).HasMaxLength(30).IsRequired();
            e.Property(a => a.RecordId).HasMaxLength(20);
            e.Property(a => a.Result).HasMaxLength(10).IsRequired();
            e.Property(a => a.Details).HasMaxLength(500);
            e.Property(a => a.IpAddress).HasMaxLength(50);
            e.HasIndex(a => a.UserId);
            e.HasIndex(a => a.EntityName);
            e.HasIndex(a => a.Action);
            e.HasIndex(a => a.CreatedDate);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        BeforeSave();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        BeforeSave();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void BeforeSave()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            // Audit log is append-only
            if (entry.Entity is AuditLog && entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("Audit logs cannot be modified or deleted.");
            }

            // DELETE on CRM records becomes a soft delete
            if (entry.Entity is ISoftDelete soft && entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                soft.IsDeleted = true;
                soft.DeletedDate = DateTime.UtcNow;
            }

            if (entry.State == EntityState.Modified && entry.Metadata.FindProperty("ModifiedDate") != null)
            {
                entry.Property("ModifiedDate").CurrentValue = DateTime.UtcNow;
            }
        }
    }
}

public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc)) { }
}
