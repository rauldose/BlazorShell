using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorShell.Modules.Analytics.Services
{
    public class DataAggregationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DataAggregationService> _logger;
        private readonly TimeSpan _aggregationInterval = TimeSpan.FromHours(1);

        public DataAggregationService(IServiceProvider serviceProvider, ILogger<DataAggregationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Data Aggregation Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await AggregateDataAsync();
                    await Task.Delay(_aggregationInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in data aggregation service");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("Data Aggregation Service stopped");
        }

        private async Task AggregateDataAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetService<Domain.Interfaces.IAnalyticsRepository>();

            if (repository != null)
            {
                _logger.LogDebug("Running data aggregation...");
                // Perform aggregation logic
                await Task.CompletedTask;
            }
        }
    }
}
