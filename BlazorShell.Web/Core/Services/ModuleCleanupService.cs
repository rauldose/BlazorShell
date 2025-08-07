using BlazorShell.Infrastructure.Services;

namespace BlazorShell.Core.Services
{
    public class ModuleCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ModuleCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval;
        private readonly TimeSpan _inactiveThreshold;

        public ModuleCleanupService(
            IServiceProvider serviceProvider,
            ILogger<ModuleCleanupService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            // Configure from appsettings.json or use defaults
            _cleanupInterval = TimeSpan.FromMinutes(
                configuration.GetValue<int>("ModuleCleanup:IntervalMinutes", 5));
            _inactiveThreshold = TimeSpan.FromMinutes(
                configuration.GetValue<int>("ModuleCleanup:InactiveThresholdMinutes", 30));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_cleanupInterval, stoppingToken);

                    using var scope = _serviceProvider.CreateScope();
                    var lazyLoader = scope.ServiceProvider.GetRequiredService<ILazyModuleLoader>();

                    _logger.LogInformation("Running module cleanup check");
                    await lazyLoader.UnloadInactiveModulesAsync(_inactiveThreshold);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during module cleanup");
                }
            }
        }
    }

}
