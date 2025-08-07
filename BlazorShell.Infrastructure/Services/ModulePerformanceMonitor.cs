// Infrastructure/Services/ModulePerformanceMonitor.cs
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using BlazorShell.Core.Interfaces;

namespace BlazorShell.Infrastructure.Services
{
    public interface IModulePerformanceMonitor
    {
        void RecordModuleLoad(string moduleName, TimeSpan loadTime);
        void RecordModuleAccess(string moduleName);
        void RecordComponentRender(string moduleName, string componentName, TimeSpan renderTime);
        ModulePerformanceStats GetStats(string moduleName);
        IEnumerable<ModulePerformanceStats> GetAllStats();
        void StartOperation(string operationId, string moduleName, string operationType);
        void EndOperation(string operationId);
        Task<PerformanceReport> GenerateReportAsync();
    }

    public class ModulePerformanceMonitor : IModulePerformanceMonitor
    {
        private readonly ConcurrentDictionary<string, ModulePerformanceStats> _stats = new();
        private readonly ConcurrentDictionary<string, OperationContext> _activeOperations = new();
        private readonly ILogger<ModulePerformanceMonitor> _logger;

        public ModulePerformanceMonitor(ILogger<ModulePerformanceMonitor> logger)
        {
            _logger = logger;
        }

        public void RecordModuleLoad(string moduleName, TimeSpan loadTime)
        {
            var stats = GetOrCreateStats(moduleName);
            stats.LoadCount++;
            stats.TotalLoadTime = stats.TotalLoadTime.Add(loadTime);
            stats.LastLoadTime = loadTime;
            stats.LastAccessed = DateTime.UtcNow;

            if (loadTime > stats.MaxLoadTime)
                stats.MaxLoadTime = loadTime;

            if (stats.MinLoadTime == TimeSpan.Zero || loadTime < stats.MinLoadTime)
                stats.MinLoadTime = loadTime;

            _logger.LogInformation("Module {Module} loaded in {Time}ms",
                moduleName, loadTime.TotalMilliseconds);
        }

        public void RecordModuleAccess(string moduleName)
        {
            var stats = GetOrCreateStats(moduleName);
            stats.AccessCount++;
            stats.LastAccessed = DateTime.UtcNow;
        }

        public void RecordComponentRender(string moduleName, string componentName, TimeSpan renderTime)
        {
            var stats = GetOrCreateStats(moduleName);

            if (!stats.ComponentRenderTimes.ContainsKey(componentName))
            {
                stats.ComponentRenderTimes[componentName] = new ComponentPerformanceStats
                {
                    ComponentName = componentName
                };
            }

            var componentStats = stats.ComponentRenderTimes[componentName];
            componentStats.RenderCount++;
            componentStats.TotalRenderTime = componentStats.TotalRenderTime.Add(renderTime);
            componentStats.LastRenderTime = renderTime;

            if (renderTime > componentStats.MaxRenderTime)
                componentStats.MaxRenderTime = renderTime;

            if (componentStats.MinRenderTime == TimeSpan.Zero || renderTime < componentStats.MinRenderTime)
                componentStats.MinRenderTime = renderTime;

            if (renderTime.TotalMilliseconds > 100) // Log slow renders
            {
                _logger.LogWarning("Slow render detected: {Component} in module {Module} took {Time}ms",
                    componentName, moduleName, renderTime.TotalMilliseconds);
            }
        }

        public void StartOperation(string operationId, string moduleName, string operationType)
        {
            _activeOperations[operationId] = new OperationContext
            {
                ModuleName = moduleName,
                OperationType = operationType,
                StartTime = Stopwatch.StartNew()
            };
        }

        public void EndOperation(string operationId)
        {
            if (_activeOperations.TryRemove(operationId, out var operation))
            {
                operation.StartTime.Stop();
                var elapsed = operation.StartTime.Elapsed;

                var stats = GetOrCreateStats(operation.ModuleName);

                if (!stats.OperationTimes.ContainsKey(operation.OperationType))
                {
                    stats.OperationTimes[operation.OperationType] = new List<TimeSpan>();
                }

                stats.OperationTimes[operation.OperationType].Add(elapsed);

                if (elapsed.TotalMilliseconds > 500) // Log slow operations
                {
                    _logger.LogWarning("Slow operation: {Operation} in module {Module} took {Time}ms",
                        operation.OperationType, operation.ModuleName, elapsed.TotalMilliseconds);
                }
            }
        }

        public ModulePerformanceStats GetStats(string moduleName)
        {
            return _stats.TryGetValue(moduleName, out var stats)
                ? stats
                : new ModulePerformanceStats { ModuleName = moduleName };
        }

        public IEnumerable<ModulePerformanceStats> GetAllStats()
        {
            return _stats.Values.OrderByDescending(s => s.AccessCount);
        }

        public async Task<PerformanceReport> GenerateReportAsync()
        {
            var report = new PerformanceReport
            {
                GeneratedAt = DateTime.UtcNow,
                ModuleStats = GetAllStats().ToList()
            };

            // Calculate aggregates
            foreach (var stat in report.ModuleStats)
            {
                if (stat.LoadCount > 0)
                {
                    stat.AverageLoadTime = TimeSpan.FromMilliseconds(
                        stat.TotalLoadTime.TotalMilliseconds / stat.LoadCount);
                }

                foreach (var componentStat in stat.ComponentRenderTimes.Values)
                {
                    if (componentStat.RenderCount > 0)
                    {
                        componentStat.AverageRenderTime = TimeSpan.FromMilliseconds(
                            componentStat.TotalRenderTime.TotalMilliseconds / componentStat.RenderCount);
                    }
                }

                // Calculate operation averages
                foreach (var kvp in stat.OperationTimes)
                {
                    if (kvp.Value.Any())
                    {
                        var avgMs = kvp.Value.Average(t => t.TotalMilliseconds);
                        stat.AverageOperationTimes[kvp.Key] = TimeSpan.FromMilliseconds(avgMs);
                    }
                }
            }

            // Identify performance issues
            report.SlowModules = report.ModuleStats
                .Where(s => s.AverageLoadTime.TotalMilliseconds > 1000)
                .Select(s => s.ModuleName)
                .ToList();

            report.FrequentlyAccessedModules = report.ModuleStats
                .OrderByDescending(s => s.AccessCount)
                .Take(5)
                .Select(s => new { s.ModuleName, s.AccessCount })
                .ToDictionary(x => x.ModuleName, x => x.AccessCount);

            return await Task.FromResult(report);
        }

        private ModulePerformanceStats GetOrCreateStats(string moduleName)
        {
            return _stats.GetOrAdd(moduleName, _ => new ModulePerformanceStats
            {
                ModuleName = moduleName,
                FirstAccessed = DateTime.UtcNow
            });
        }

        private class OperationContext
        {
            public string ModuleName { get; set; } = string.Empty;
            public string OperationType { get; set; } = string.Empty;
            public Stopwatch StartTime { get; set; } = null!;
        }
    }

    public class ModulePerformanceStats
    {
        public string ModuleName { get; set; } = string.Empty;
        public int LoadCount { get; set; }
        public int AccessCount { get; set; }
        public TimeSpan TotalLoadTime { get; set; }
        public TimeSpan LastLoadTime { get; set; }
        public TimeSpan MinLoadTime { get; set; }
        public TimeSpan MaxLoadTime { get; set; }
        public TimeSpan AverageLoadTime { get; set; }
        public DateTime FirstAccessed { get; set; }
        public DateTime LastAccessed { get; set; }

        public Dictionary<string, ComponentPerformanceStats> ComponentRenderTimes { get; set; } = new();
        public Dictionary<string, List<TimeSpan>> OperationTimes { get; set; } = new();
        public Dictionary<string, TimeSpan> AverageOperationTimes { get; set; } = new();
    }

    public class ComponentPerformanceStats
    {
        public string ComponentName { get; set; } = string.Empty;
        public int RenderCount { get; set; }
        public TimeSpan TotalRenderTime { get; set; }
        public TimeSpan LastRenderTime { get; set; }
        public TimeSpan MinRenderTime { get; set; }
        public TimeSpan MaxRenderTime { get; set; }
        public TimeSpan AverageRenderTime { get; set; }
    }

    public class PerformanceReport
    {
        public DateTime GeneratedAt { get; set; }
        public List<ModulePerformanceStats> ModuleStats { get; set; } = new();
        public List<string> SlowModules { get; set; } = new();
        public Dictionary<string, int> FrequentlyAccessedModules { get; set; } = new();
    }
}