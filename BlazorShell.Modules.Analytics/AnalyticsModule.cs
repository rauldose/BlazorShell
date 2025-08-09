// BlazorShell.Modules.Analytics/AnalyticsModule.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using BlazorShell.Application.Interfaces;
using BlazorShell.Domain.Entities;
using BlazorShell.Modules.Analytics.Domain.Interfaces;
using BlazorShell.Modules.Analytics.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Components;
using System.Reflection;
using Microsoft.Extensions.Options;
using BlazorShell.Modules.Analytics.Configuration;
using Microsoft.JSInterop;

namespace BlazorShell.Modules.Analytics
{
    public class AnalyticsModule : IModule, IServiceModule
    {
        private ILogger<AnalyticsModule> _logger;
        private IServiceProvider _serviceProvider;
        private bool _isInitialized = false;
        private bool _isActive = false;
        private static ModuleCdnLoader _cdnLoader; // Static to persist across requests
        private static bool _cdnResourcesLoaded = false;
        // IModule Properties
        public string Name => "Analytics";
        public string DisplayName => "Sales Analytics";
        public string Description => "Comprehensive sales analytics and business intelligence dashboard with real-time reporting capabilities";
        public string Version => "1.0.0";
        public string Author => "Enterprise Team";
        public string Icon => "bi bi-graph-up";
        public string Category => "Business Intelligence";
        public int Order => 100;

        public async Task<bool> InitializeAsync(IServiceProvider serviceProvider)
        {
            try
            {
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
                _logger = _serviceProvider.GetService<ILogger<AnalyticsModule>>();


                var cfg = _serviceProvider.GetRequiredService<IConfiguration>();

                // Pull module settings directly from config (DI options may not be wired yet)
                var settings = cfg.GetSection("Modules:Analytics").Get<AnalyticsSettings>()
                              ?? new AnalyticsSettings(); // your POCO with sensible defaults

                // Resolve connection string
                string connectionString;
                if (settings.UseSeparateDatabase)
                {
                    connectionString = settings.ConnectionString
                                       ?? cfg["Modules:Analytics:ConnectionString"]
                                       ?? "Server=localhost;Database=BlazorShell_Analytics;Trusted_Connection=True;TrustServerCertificate=True";
                }
                else
                {
                    connectionString = cfg.GetConnectionString("DefaultConnection")
                                       ?? throw new InvalidOperationException("DefaultConnection not configured.");
                }

                // Build a one-off DbContext to create/migrate DB
                var dbOptions = new DbContextOptionsBuilder<AnalyticsDbContext>();

                // Match whatever provider you support (default SqlServer here)
                ConfigureDbOptions(dbOptions, settings.DatabaseProvider, connectionString);

                using (var db = new AnalyticsDbContext(dbOptions.Options))
                {
                    if (settings.EnableMigrations)
                    {
                        _logger?.LogInformation("Running Analytics migrations…");
                        await db.Database.MigrateAsync();
                    }
                    else
                    {
                        _logger?.LogInformation("Ensuring Analytics database exists…");
                        await db.Database.EnsureCreatedAsync();
                    }

                    if (settings.SeedSampleData)
                    {
                        var repoLogger = _serviceProvider.GetService<ILogger<AnalyticsRepository>>();
                        var repo = new AnalyticsRepository(db, repoLogger);

                        if (!await repo.HasDataAsync())
                        {
                            _logger?.LogInformation("Seeding Analytics sample data…");
                            await SeedSampleDataAsync(db); // your existing seeding method
                        }
                        else
                        {
                            _logger?.LogInformation("Seed skipped: data already present.");
                        }
                    }
                }

                _logger?.LogInformation("Initializing Analytics module v{Version}…", Version);
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize Analytics module");
                return false;
            }
        }
        private void ConfigureDbOptions(DbContextOptionsBuilder options, string provider, string cs)
        {
            switch ((provider ?? "SqlServer").Trim().ToLowerInvariant())
            {
                // case "sqlite": options.UseSqlite(cs); break;
                // case "postgresql":
                // case "npgsql": options.UseNpgsql(cs); break;
                // case "mysql":
                // case "mariadb": options.UseMySql(cs, ServerVersion.AutoDetect(cs)); break;
                default:
                    options.UseSqlServer(cs, sql =>
                    {
                        sql.MigrationsAssembly("BlazorShell.Modules.Analytics");
                        sql.MigrationsHistoryTable("__AnalyticsMigrationsHistory", "Analytics");
                    });
                    break;
            }
        }
        public async Task<bool> ActivateAsync()
        {
            try
            {
                //if (!_isInitialized)
                //{
                //    _logger?.LogWarning("Cannot activate Analytics module - not initialized");
                //    return false;
                //}

                _logger?.LogInformation("Activating Analytics module...");
                // Ensure DB before starting background bits

                // Start any background services
                await StartBackgroundServicesAsync();

                // Initialize cache warming
                await WarmupCacheAsync();

                _isActive = true;
                _logger?.LogInformation("Analytics module activated successfully");

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to activate Analytics module");
                return false;
            }
        }

        public async Task<bool> DeactivateAsync()
        {
            try
            {
                _logger?.LogInformation("Deactivating Analytics module...");

                // Stop background services
                await StopBackgroundServicesAsync();

                // Clear cache
                await ClearCacheAsync();
                // Unload CDN resources if loaded
                if (_cdnLoader != null && _cdnResourcesLoaded)
                {
                    await _cdnLoader.UnloadResourcesAsync();
                    _cdnResourcesLoaded = false;
                    _logger?.LogInformation("CDN resources unloaded");
                }

                _isActive = false;
                _logger?.LogInformation("Analytics module deactivated successfully");

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to deactivate Analytics module");
                return false;
            }
        }

        public IEnumerable<NavigationItem> GetNavigationItems()
        {
            var items = new List<NavigationItem>();

            // Main Analytics menu item (parent)
            var analyticsMenu = new NavigationItem
            {
                Name = "analytics",
                DisplayName = "Analytics",
                Icon = "bi bi-graph-up",
                Order = 100,
                IsVisible = true,
                ParentId = null,
                Type = NavigationType.SideMenu,
                IsPublic = false,
                MinimumRole = "User",
                CssClass = "analytics-menu",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "AnalyticsModule"
            };
            items.Add(analyticsMenu);
             var children = new List<NavigationItem>();
            // Dashboard - main landing page
            children.Add(new NavigationItem
            {
                Name = "analytics-dashboard",
                DisplayName = "Dashboard",
                Url = "/analytics",
                Icon = "bi bi-speedometer2",
                ParentId = null, // Will be set when saved to DB with parent reference
                Order = 1,
                IsVisible = true,
                Type = NavigationType.SideMenu,
                IsPublic = false,
                MinimumRole = "User",
                Target = "_self",
                CssClass = "analytics-dashboard-nav",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "AnalyticsModule"
            });

            // Reports page
            children.Add(new NavigationItem
            {
                Name = "analytics-reports",
                DisplayName = "Reports",
                Url = "/analytics/reports",
                Icon = "bi bi-file-earmark-bar-graph",
                ParentId = null,
                Order = 2,
                IsVisible = true,
                Type = NavigationType.SideMenu,
                IsPublic = false,
                MinimumRole = "Analyst",
                Target = "_self",
                CssClass = "analytics-reports-nav",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "AnalyticsModule"
            });

            // Real-time Monitor
            children.Add(new NavigationItem
            {
                Name = "analytics-realtime",
                DisplayName = "Real-time Monitor",
                Url = "/analytics/realtime",
                Icon = "bi bi-activity",
                ParentId = null,
                Order = 3,
                IsVisible = true,
                Type = NavigationType.SideMenu,
                IsPublic = false,
                MinimumRole = "Analyst",
                Target = "_self",
                CssClass = "analytics-realtime-nav",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "AnalyticsModule"
            });

            // Export Center
            children.Add(new NavigationItem
            {
                Name = "analytics-export",
                DisplayName = "Export Center",
                Url = "/analytics/export",
                Icon = "bi bi-download",
                ParentId = null,
                Order = 4,
                IsVisible = true,
                Type = NavigationType.SideMenu,
                IsPublic = false,
                MinimumRole = "User",
                Target = "_self",
                CssClass = "analytics-export-nav",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "AnalyticsModule"
            });

            // Scheduled Reports
            children.Add(new NavigationItem
            {
                Name = "analytics-scheduled",
                DisplayName = "Scheduled Reports",
                Url = "/analytics/scheduled",
                Icon = "bi bi-calendar-check",
                ParentId = null,
                Order = 5,
                IsVisible = true,
                Type = NavigationType.SideMenu,
                IsPublic = false,
                MinimumRole = "Manager",
                Target = "_self",
                CssClass = "analytics-scheduled-nav",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "AnalyticsModule"
            });

            // Settings (admin only)
            children.Add(new NavigationItem
            {
                Name = "analytics-settings",
                DisplayName = "Settings",
                Url = "/analytics/settings",
                Icon = "bi bi-gear",
                ParentId = null,
                Order = 99,
                IsVisible = true,
                Type = NavigationType.SideMenu,
                IsPublic = false,
                MinimumRole = "Administrator",
                Target = "_self",
                CssClass = "analytics-settings-nav",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "AnalyticsModule"
            });

            // External link example - Documentation
            children.Add(new NavigationItem
            {
                Name = "analytics-docs",
                DisplayName = "Documentation",
                Url = "https://docs.example.com/analytics",
                Icon = "bi bi-question-circle",
                ParentId = null,
                Order = 100,
                IsVisible = true,
                Type = NavigationType.SideMenu,
                IsPublic = true,
                Target = "_blank",
                CssClass = "analytics-docs-nav",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "AnalyticsModule"
            });
            analyticsMenu.Children = children;
            return items;
        }

        public IEnumerable<Type> GetComponentTypes()
        {
            // Return all Blazor components provided by this module
            var assembly = typeof(AnalyticsModule).Assembly;
            return assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(ComponentBase)) &&
                           !t.IsAbstract &&
                           t.GetCustomAttributes<RouteAttribute>().Any())
                .ToList();
        }

        public Dictionary<string, object> GetDefaultSettings()
        {
            return new Dictionary<string, object>
            {
                // Display settings
                ["DefaultDateRange"] = "Last30Days",
                ["DefaultChartType"] = "Line",
                ["ShowRealTimeUpdates"] = true,
                ["AutoRefreshInterval"] = 30000, // milliseconds

                // Performance settings
                ["EnableCaching"] = true,
                ["CacheDurationMinutes"] = 5,
                ["MaxDataPoints"] = 1000,
                ["EnableDataCompression"] = true,

                // Export settings
                ["EnablePdfExport"] = true,
                ["EnableExcelExport"] = true,
                ["EnableCsvExport"] = true,
                ["DefaultExportFormat"] = "PDF",

                // Chart settings
                ["ChartAnimationDuration"] = 750,
                ["ChartColorScheme"] = "Default",
                ["ShowChartLegends"] = true,
                ["ShowChartTooltips"] = true,

                // Data settings
                ["EnableSampleData"] = false,
                ["DataRetentionDays"] = 365,
                ["EnableDataAggregation"] = true,
                ["AggregationInterval"] = "Daily",

                // Feature flags
                ["EnableAdvancedAnalytics"] = true,
                ["EnablePredictiveAnalytics"] = false,
                ["EnableAnomalyDetection"] = false,
                ["EnableCustomReports"] = true,

                // Database settings
                ["UseSeparateDatabase"] = true,
                ["DatabaseProvider"] = "SqlServer",
                ["EnableMigrations"] = false,
                ["SeedSampleData"] = true,

                // API settings
                ["EnableApiAccess"] = true,
                ["ApiRateLimit"] = 100,
                ["ApiCacheDuration"] = 60,

                // Notification settings
                ["EnableAlerts"] = true,
                ["AlertThresholds"] = new Dictionary<string, object>
                {
                    ["LowSalesThreshold"] = 1000,
                    ["HighRevenueThreshold"] = 100000,
                    ["AnomalyDetectionSensitivity"] = 0.8
                }
            };
        }

        // Private helper methods
        private void ConfigureModuleServices(IServiceCollection services, IServiceProvider rootProvider)
        {
            // Get configuration from root provider if available
            var configuration = rootProvider.GetService<Microsoft.Extensions.Configuration.IConfiguration>();

            // Configure database context
           
        }
        private  async Task EnsureDatabaseReadyAsync(IServiceProvider root)
        {
            using var scope = root.CreateScope();
            var sp = scope.ServiceProvider;

            var db = sp.GetRequiredService<AnalyticsDbContext>();
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AnalyticsSettings>>().Value;
            var logger = sp.GetRequiredService<ILogger<AnalyticsModule>>();

            if (settings.EnableMigrations)
            {
                logger.LogInformation("Running Analytics migrations…");
                await db.Database.MigrateAsync();
            }
            else
            {
                logger.LogInformation("Ensuring Analytics database exists…");
                await db.Database.EnsureCreatedAsync();
            }

            if (settings.SeedSampleData)
            {
         
                
                    logger.LogInformation("Seeding Analytics sample data…");
                    await SeedSampleDataAsync(db); // reuse your current logic
                
            }
        }

        private void ConfigureDatabase(DbContextOptionsBuilder options, string provider, string connectionString)
        {
            switch (provider.ToLower())
            {
                //case "sqlite":
                //    options.UseSqlite(connectionString);
                //    break;
                //case "postgresql":
                //    options.UseNpgsql(connectionString);
                //    break;
                //case "mysql":
                //    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                //    break;
                //case "inmemory":
                //    options.UseInMemoryDatabase("AnalyticsDb");
                //    break;
                default: // SqlServer
                    options.UseSqlServer(connectionString, sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly("BlazorShell.Modules.Analytics");
                        sqlOptions.MigrationsHistoryTable("__AnalyticsMigrationsHistory", "Analytics");
                    });
                    break;
            }
        }

        private async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetService<AnalyticsDbContext>();

                if (dbContext == null)
                {
                    _logger?.LogWarning("AnalyticsDbContext not found in service provider");
                    return;
                }

                var settings = GetDefaultSettings();
                var enableMigrations = (bool)settings["EnableMigrations"];

                if (enableMigrations)
                {
                    _logger?.LogInformation("Running database migrations for Analytics module...");
                    await dbContext.Database.MigrateAsync();
                }
                else
                {
                    _logger?.LogInformation("Ensuring database created for Analytics module...");
                    await dbContext.Database.EnsureCreatedAsync();
                }

                // Seed sample data if configured
                var seedData = (bool)settings["SeedSampleData"];
                if (seedData)
                {
                    await SeedSampleDataAsync(dbContext);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize Analytics database");
                throw;
            }
        }

        private async Task SeedSampleDataAsync(AnalyticsDbContext dbContext)
        {
            try
            {
                var repository = new AnalyticsRepository(dbContext,
                    _serviceProvider.GetService<ILogger<AnalyticsRepository>>());

                if (await repository.HasDataAsync())
                {
                    _logger?.LogInformation("Analytics database already contains data, skipping seed");
                    return;
                }

                _logger?.LogInformation("Seeding sample data for Analytics module...");

                var random = new Random();
                var categories = new[] { "Electronics", "Clothing", "Food", "Books", "Home" };
                var regions = new[] { "North", "South", "East", "West", "Central" };
                var channels = new[] { "Online", "Retail", "Wholesale" };
                var segments = new[] { "Enterprise", "SMB", "Consumer" };
                var reps = new[] { "John Smith", "Jane Doe", "Bob Johnson", "Alice Brown", "Charlie Wilson" };

                var sampleData = new List<Domain.Entities.SalesData>();
                var startDate = DateTime.Now.AddMonths(-12);

                for (int i = 0; i < 5000; i++)
                {
                    var revenue = (decimal)(random.Next(100, 10000) + random.NextDouble());
                    var costPercentage = (decimal)(0.3 + (random.NextDouble() * 0.4));

                    sampleData.Add(new Domain.Entities.SalesData
                    {
                        Date = startDate.AddDays(random.Next(0, 365)),
                        ProductCategory = categories[random.Next(categories.Length)],
                        Region = regions[random.Next(regions.Length)],
                        SalesRepresentative = reps[random.Next(reps.Length)],
                        Revenue = revenue,
                        Quantity = random.Next(1, 100),
                        Cost = revenue * costPercentage,
                        Channel = channels[random.Next(channels.Length)],
                        CustomerSegment = segments[random.Next(segments.Length)]
                    });
                }

                await repository.BulkInsertAsync(sampleData);
                _logger?.LogInformation("Successfully seeded {Count} sample records", sampleData.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to seed sample data");
            }
        }

        private async Task<bool> ValidateDependenciesAsync(IServiceProvider serviceProvider)
        {
            try
            {
                // Check for required services
                var requiredServices = new[]
                {
                    typeof(ILogger<AnalyticsModule>),
                    typeof(Microsoft.Extensions.Caching.Memory.IMemoryCache)
                };

                foreach (var serviceType in requiredServices)
                {
                    var service = serviceProvider.GetService(serviceType);
                    if (service == null)
                    {
                        _logger?.LogWarning("Required service {ServiceType} not found", serviceType.Name);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to validate dependencies");
                return false;
            }
        }

        private async Task LoadModuleConfigurationAsync(IServiceProvider serviceProvider)
        {
            try
            {
                // Load any persisted configuration overrides
                var configuration = serviceProvider.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                if (configuration != null)
                {
                    var moduleConfig = configuration.GetSection($"Modules:{Name}");
                    if (moduleConfig.Exists())
                    {
                        _logger?.LogInformation("Loading configuration overrides for Analytics module");
                        // Apply configuration overrides to default settings
                        // This would typically merge with GetDefaultSettings()
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load module configuration");
            }
        }

        private readonly CancellationTokenSource _bgCts = new();

        private async Task StartBackgroundServicesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var sp = scope.ServiceProvider;

            var dataAgg = sp.GetRequiredService<DataAggregationService>();
            var alerts = sp.GetRequiredService<AlertMonitoringService>();

            // Manually start them; BackgroundService implements IHostedService
            await dataAgg.StartAsync(_bgCts.Token);
            await alerts.StartAsync(_bgCts.Token);
        }

        private async Task StopBackgroundServicesAsync()
        {
            _bgCts.Cancel();

            using var scope = _serviceProvider.CreateScope();
            var sp = scope.ServiceProvider;

            // Stop in reverse order, swallow cancellation
            var alerts = sp.GetService<AlertMonitoringService>();
            var dataAgg = sp.GetService<DataAggregationService>();

            if (alerts != null) await alerts.StopAsync(CancellationToken.None);
            if (dataAgg != null) await dataAgg.StopAsync(CancellationToken.None);
        }

        private async Task WarmupCacheAsync()
        {
            try
            {
                var settings = GetDefaultSettings();
                if (!(bool)settings["EnableCaching"])
                {
                    return;
                }

                _logger?.LogInformation("Warming up Analytics cache...");

                // Pre-load frequently accessed data into cache
                using var scope = _serviceProvider.CreateScope();
                var analyticsService = scope.ServiceProvider.GetService<IAnalyticsService>();

                if (analyticsService != null)
                {
                    // Load current month's data
                    var startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    var endDate = DateTime.Now;

                    await analyticsService.GetDashboardMetricsAsync(startDate, endDate);
                    _logger?.LogInformation("Cache warmup completed");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to warmup cache");
                // Don't throw - cache warmup failure shouldn't prevent activation
            }
        }

        private async Task ClearCacheAsync()
        {
            try
            {
                _logger?.LogInformation("Clearing Analytics cache...");

                var cache = _serviceProvider.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
                if (cache is Microsoft.Extensions.Caching.Memory.MemoryCache memoryCache)
                {
                    memoryCache.Compact(1.0); // Clear all cache entries
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to clear cache");
            }
        }

        public void RegisterServices(IServiceCollection services)
        {
            services.AddOptions<AnalyticsSettings>()
             .BindConfiguration("Modules:Analytics")
             .ValidateDataAnnotations()
             .ValidateOnStart();

            // 2) DbContext with provider-aware overload (no BuildServiceProvider!)
            services.AddDbContext<AnalyticsDbContext>((sp, options) =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var settings = sp.GetRequiredService<IOptions<AnalyticsSettings>>().Value;

                if (settings.UseSeparateDatabase)
                {
                    var connectionString =
                        settings.ConnectionString
                        ?? configuration["Modules:Analytics:ConnectionString"]
                        ?? "Server=localhost;Database=BlazorShell_Analytics;Trusted_Connection=True;TrustServerCertificate=True";

                    ConfigureDatabase(options, settings.DatabaseProvider, connectionString);
                }
                else
                {
                    var cs = configuration.GetConnectionString("DefaultConnection");
                    options.UseSqlServer(cs, sql =>
                    {
                        sql.MigrationsAssembly("BlazorShell.Modules.Analytics");
                        sql.MigrationsHistoryTable("__AnalyticsMigrationsHistory", "Analytics");
                    });
                }
            });

            // 3) Repositories & services
            services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
            services.AddScoped<IAnalyticsService, AnalyticsService>();
            services.AddScoped<IReportingService, ReportingService>();
            services.AddScoped<IExportService, ExportService>();
            services.AddSingleton<ICacheService, CacheService>();

            // 4) Move async init to a one-shot bootstrapper
            //services.AddHostedService<AnalyticsBootstrapper>();
            //services.AddSingleton<ModuleCdnLoader>(sp =>
            //{
            //    var jsRuntime = sp.GetService<IJSRuntime>();
            //    var logger = sp.GetService<ILogger<ModuleCdnLoader>>();

            //    // Return existing loader if available, otherwise create new
            //    if (_cdnLoader != null)
            //    {
            //        return _cdnLoader;
            //    }

            //    return new ModuleCdnLoader(jsRuntime, logger);
            //});
            services.AddScoped<ModuleCdnLoader>();
            // Register lazy CDN initializer for components
            services.AddScoped<ICdnResourceManager, CdnResourceManager>();
            //// Keep your continuous background services
            //services.AddHostedService<DataAggregationService>();
            //services.AddHostedService<AlertMonitoringService>();
        }
    }
}