using BlazorShell.Modules.Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class AnalyticsDbContext : DbContext
{
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options)
        : base(options)
    {
    }

    public DbSet<SalesData> SalesData { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("Analytics");
        // Configure SalesData entity
        modelBuilder.Entity<SalesData>(entity =>
        {
            entity.ToTable("SalesData", "Analytics");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Date)
                .IsRequired();

            entity.Property(e => e.ProductCategory)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Region)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.SalesRepresentative)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Revenue)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.Cost)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.Quantity)
                .IsRequired();

            entity.Property(e => e.Channel)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.CustomerSegment)
                .IsRequired()
                .HasMaxLength(50);

            // Computed column for Profit
            entity.Ignore(e => e.Profit);

            // Indexes for better query performance
            entity.HasIndex(e => e.Date)
                .HasDatabaseName("IX_SalesData_Date");

            entity.HasIndex(e => e.ProductCategory)
                .HasDatabaseName("IX_SalesData_ProductCategory");

            entity.HasIndex(e => e.Region)
                .HasDatabaseName("IX_SalesData_Region");

            entity.HasIndex(e => e.SalesRepresentative)
                .HasDatabaseName("IX_SalesData_SalesRepresentative");

            entity.HasIndex(e => new { e.Date, e.ProductCategory })
                .HasDatabaseName("IX_SalesData_Date_ProductCategory");
        });
    }
}