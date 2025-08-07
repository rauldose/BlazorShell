using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BlazorShell.Infrastructure.Data;
using BlazorShell.Core.Entities;
using BlazorShell.Core.Interfaces;

namespace BlazorShell.Infrastructure.Services
{
    public interface IApplicationStartupService
    {
        Task InitializeApplicationAsync();
    }

    public class ApplicationStartupService : IApplicationStartupService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ApplicationStartupService> _logger;
        private readonly IDatabaseInitializationService _databaseService;
        private readonly IModuleInitializationService _moduleService;

        public ApplicationStartupService(
            IServiceProvider serviceProvider,
            IWebHostEnvironment environment,
            ILogger<ApplicationStartupService> logger,
            IDatabaseInitializationService databaseService,
            IModuleInitializationService moduleService)
        {
            _serviceProvider = serviceProvider;
            _environment = environment;
            _logger = logger;
            _databaseService = databaseService;
            _moduleService = moduleService;
        }

        public async Task InitializeApplicationAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                _logger.LogInformation("Starting application initialization");

                // Initialize database
                await _databaseService.InitializeDatabaseAsync(services);

                // Initialize modules
                await _moduleService.InitializeModulesAsync(services);

                _logger.LogInformation("Application initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during application initialization");
                throw;
            }
        }
    }
}