using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using BlazorShell.Core.Entities;
using System.Reflection;

namespace BlazorShell.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>, IDataProtectionKeyContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Core entities
        public DbSet<Core.Entities.Module> Modules { get; set; }
        public DbSet<ModulePermission> ModulePermissions { get; set; }
        public DbSet<NavigationItem> NavigationItems { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Setting> Settings { get; set; }

        // Data protection keys for distributed scenarios
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Apply configurations from assembly
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Rename Identity tables
            builder.Entity<ApplicationUser>().ToTable("Users");
            builder.Entity<ApplicationRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

            // Add indexes for performance
            builder.Entity<AuditLog>()
                .HasIndex(e => e.CreatedDate)
                .HasDatabaseName("IX_AuditLog_CreatedDate");

            builder.Entity<Core.Entities.Module>()
                .HasIndex(e => e.IsEnabled)
                .HasDatabaseName("IX_Module_IsEnabled");
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ProcessAuditableEntities();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void ProcessAuditableEntities()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is IAuditableEntity &&
                           (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (IAuditableEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedDate = DateTime.UtcNow;
                    entity.CreatedBy = "DEFAULT";
                }
                else
                {
                    entity.ModifiedDate = DateTime.UtcNow;
                    entity.ModifiedBy = "DEFAULT";
                    // Prevent modification of CreatedDate
                    entry.Property(nameof(IAuditableEntity.CreatedDate)).IsModified = false;
                }
            }
        }
    }
}