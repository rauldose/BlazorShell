using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using BlazorShell.Core.Interfaces;
using BlazorShell.Core.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Infrastructure.Services;

public class ModuleHotReloadService : IModuleHotReloadService, IHostedService, IDisposable
{
    private readonly IModuleLoader _moduleLoader;
    private readonly ILogger<ModuleHotReloadService> _logger;
    private readonly IHostEnvironment _environment;
    private readonly ConcurrentDictionary<string, FileSystemWatcher> _watchers = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastReloadTimes = new();
    private readonly TimeSpan _reloadDebounceTime = TimeSpan.FromSeconds(2);

    public event EventHandler<ModuleChangedEventArgs>? ModuleChanged;

    public ModuleHotReloadService(
        IModuleLoader moduleLoader,
        ILogger<ModuleHotReloadService> logger,
        IHostEnvironment environment)
    {
        _moduleLoader = moduleLoader;
        _logger = logger;
        _environment = environment;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation("Module hot reload service started in development mode");
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return StopAllWatchersAsync();
    }

    public async Task StartWatchingAsync(string moduleName, string assemblyPath)
    {
        if (!_environment.IsDevelopment())
        {
            _logger.LogWarning("Hot reload is only available in development mode");
            return;
        }

        if (_watchers.ContainsKey(moduleName))
        {
            _logger.LogWarning("Already watching module {Module}", moduleName);
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(assemblyPath);
            var fileName = Path.GetFileName(assemblyPath);

            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                _logger.LogError("Invalid assembly path for module {Module}: {Path}", moduleName, assemblyPath);
                return;
            }

            var watcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            watcher.Changed += async (sender, e) => await OnFileChanged(moduleName, e);
            watcher.Created += async (sender, e) => await OnFileChanged(moduleName, e);

            _watchers[moduleName] = watcher;
            _logger.LogInformation("Started watching module {Module} at {Path}", moduleName, assemblyPath);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting watcher for module {Module}", moduleName);
        }
    }

    public async Task StopWatchingAsync(string moduleName)
    {
        if (_watchers.TryRemove(moduleName, out var watcher))
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            _logger.LogInformation("Stopped watching module {Module}", moduleName);
        }

        await Task.CompletedTask;
    }

    public async Task StopAllWatchersAsync()
    {
        foreach (var moduleName in _watchers.Keys.ToList())
        {
            await StopWatchingAsync(moduleName);
        }
    }

    public bool IsWatching(string moduleName) => _watchers.ContainsKey(moduleName);

    private async Task OnFileChanged(string moduleName, FileSystemEventArgs e)
    {
        try
        {
            if (_lastReloadTimes.TryGetValue(moduleName, out var lastReload))
            {
                if (DateTime.UtcNow - lastReload < _reloadDebounceTime)
                {
                    _logger.LogDebug("Skipping reload for {Module} due to debounce", moduleName);
                    return;
                }
            }

            _lastReloadTimes[moduleName] = DateTime.UtcNow;

            _logger.LogInformation("Detected change in module {Module}, reloading...", moduleName);

            await Task.Delay(500);

            await _moduleLoader.ReloadModuleAsync(moduleName);

            ModuleChanged?.Invoke(this, new ModuleChangedEventArgs
            {
                ModuleName = moduleName,
                ChangeType = FileChangeType.Modified,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("Module {Module} reloaded successfully", moduleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading module {Module}", moduleName);
        }
    }

    public void Dispose()
    {
        foreach (var watcher in _watchers.Values)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _watchers.Clear();
    }
}
