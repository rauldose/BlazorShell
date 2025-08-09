// BlazorShell.Modules.Analytics/Infrastructure/AnalyticsBootstrapper.cs
using BlazorShell.Modules.Analytics;
using BlazorShell.Modules.Analytics.Configuration;
using BlazorShell.Modules.Analytics.Domain.Interfaces;
using BlazorShell.Modules.Analytics.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class AnalyticsBootstrapper : IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<AnalyticsBootstrapper> _logger;
    private readonly IOptions<AnalyticsSettings> _settings;

    public AnalyticsBootstrapper(
        IServiceProvider sp,
        ILogger<AnalyticsBootstrapper> logger,
        IOptions<AnalyticsSettings> settings)
    {
        _sp = sp;
        _logger = logger;
        _settings = settings;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _sp.CreateScope();
        var services = scope.ServiceProvider;

        // 1) Validate dependencies
        if (!await ValidateDependenciesAsync(services))
        {
            _logger.LogWarning("Analytics module dependencies validation failed — skipping bootstrap.");
            return;
        }

        // 2) Load per-module configuration overrides (optional)
        await LoadModuleConfigurationAsync(services);

        // 3) Initialize database (migrate/ensure + seed if needed)
        await InitializeDatabaseAsync(services, cancellationToken);

        // 4) Cache warmup (optional)
        await WarmupCacheAsync(services, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Optional: clear/compact cache, stop timers, etc.
        return Task.CompletedTask;
    }

    private static async Task<bool> ValidateDependenciesAsync(IServiceProvider services)
    {
        var logger = services.GetService<ILogger<AnalyticsBootstrapper>>();
        var required = new[]
        {
            typeof(ILogger<AnalyticsModule>), // if you rely on it elsewhere
            typeof(IMemoryCache)
        };

        foreach (var type in required)
        {
            if (services.GetService(type) is null)
            {
                logger?.LogWarning("Required service {ServiceType} not found.", type.Name);
                return false;
            }
        }
        return await Task.FromResult(true);
    }

    private static Task LoadModuleConfigurationAsync(IServiceProvider services)
    {
        var logger = services.GetService<ILogger<AnalyticsBootstrapper>>();
        var configuration = services.GetService<IConfiguration>();
        if (configuration is not null)
        {
            var section = configuration.GetSection("Modules:Analytics");
            if (section.Exists())
            {
                logger?.LogInformation("Loaded configuration overrides for Analytics module.");
                // If you need to merge into some runtime state, do it here.
            }
        }
        return Task.CompletedTask;
    }

    private async Task InitializeDatabaseAsync(IServiceProvider services, CancellationToken ct)
    {
        var logger = services.GetRequiredService<ILogger<AnalyticsBootstrapper>>();
        var db = services.GetService<AnalyticsDbContext>();
        if (db is null)
        {
            logger.LogWarning("AnalyticsDbContext not found in service provider.");
            return;
        }

        if (_settings.Value.EnableMigrations)
        {
            logger.LogInformation("Running database migrations for Analytics module...");
            await db.Database.MigrateAsync(ct);
        }
        else
        {
            logger.LogInformation("Ensuring database for Analytics module...");
            await db.Database.EnsureCreatedAsync(ct);
        }

        if (_settings.Value.SeedSampleData)
        {
            var repoLogger = services.GetService<ILogger<AnalyticsRepository>>();
            var repo = new AnalyticsRepository(db, repoLogger);

            if (!await repo.HasDataAsync())
            {
                logger.LogInformation("Seeding sample data for Analytics module...");
                await SeedSampleDataAsync(repo, logger, ct);
                logger.LogInformation("Sample data seeded.");
            }
            else
            {
                logger.LogInformation("Analytics database already contains data — skipping seed.");
            }
        }
    }

    private static async Task SeedSampleDataAsync(
        IAnalyticsRepository repo,
        ILogger logger,
        CancellationToken ct)
    {
        // === Paste your existing sample generation here ===
        // I kept your original structure, trimmed for brevity.
        var rnd = new Random();
        var categories = new[] { "Electronics", "Clothing", "Food", "Books", "Home" };
        var regions = new[] { "North", "South", "East", "West", "Central" };
        var channels = new[] { "Online", "Retail", "Wholesale" };
        var segments = new[] { "Enterprise", "SMB", "Consumer" };
        var reps = new[] { "John Smith", "Jane Doe", "Bob Johnson", "Alice Brown", "Charlie Wilson" };

        var startDate = DateTime.Now.AddMonths(-12);
        var list = new List<BlazorShell.Modules.Analytics.Domain.Entities.SalesData>();

        for (int i = 0; i < 5000; i++)
        {
            var revenue = (decimal)(rnd.Next(100, 10000) + rnd.NextDouble());
            var costPct = (decimal)(0.3 + (rnd.NextDouble() * 0.4));
            list.Add(new BlazorShell.Modules.Analytics.Domain.Entities.SalesData
            {
                Date = startDate.AddDays(rnd.Next(0, 365)),
                ProductCategory = categories[rnd.Next(categories.Length)],
                Region = regions[rnd.Next(regions.Length)],
                SalesRepresentative = reps[rnd.Next(reps.Length)],
                Revenue = revenue,
                Quantity = rnd.Next(1, 100),
                Cost = revenue * costPct,
                Channel = channels[rnd.Next(channels.Length)],
                CustomerSegment = segments[rnd.Next(segments.Length)]
            });
        }

        await repo.BulkInsertAsync(list);
        logger.LogInformation("Seeded {Count} sample records.", list.Count);
    }

    private static async Task WarmupCacheAsync(IServiceProvider services, CancellationToken ct)
    {
        var logger = services.GetService<ILogger<AnalyticsBootstrapper>>();
        var options = services.GetService<IOptions<AnalyticsSettings>>();
        if (options?.Value.EnableCaching != true) return;

        var svc = services.GetService<IAnalyticsService>();
        if (svc is null) return;

        logger?.LogInformation("Warming up Analytics cache...");
        var start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var end = DateTime.Now;
        await svc.GetDashboardMetricsAsync(start, end);
        logger?.LogInformation("Cache warmup completed.");
    }
}
