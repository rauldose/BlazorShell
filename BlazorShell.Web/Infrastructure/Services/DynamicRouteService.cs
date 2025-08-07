// Infrastructure/Services/DynamicRouteService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using BlazorShell.Core.Interfaces;

namespace BlazorShell.Infrastructure.Services
{

    public class DynamicRouteService
    {
        private readonly ILogger<DynamicRouteService> _logger;
        private readonly Dictionary<string, Assembly> _moduleAssemblies = new();
        private readonly Dictionary<string, RouteInfo> _routes = new();
        private readonly ReaderWriterLockSlim _lock = new();

        public event EventHandler<RouteChangedEventArgs>? RoutesChanged;

        public IReadOnlyList<Assembly> ModuleAssemblies
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _moduleAssemblies.Values.ToList();
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public IReadOnlyDictionary<string, RouteInfo> Routes
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return new Dictionary<string, RouteInfo>(_routes);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public DynamicRouteService(ILogger<DynamicRouteService> logger)
        {
            _logger = logger;
        }

        public void RegisterModuleAssembly(string moduleName, Assembly assembly)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_moduleAssemblies.ContainsKey(moduleName))
                {
                    _logger.LogWarning("Module {ModuleName} assembly already registered, replacing", moduleName);
                    UnregisterModuleAssemblyInternal(moduleName);
                }

                _moduleAssemblies[moduleName] = assembly;

                // Discover routes in the assembly
                var routes = DiscoverRoutes(assembly, moduleName);
                foreach (var route in routes)
                {
                    if (_routes.ContainsKey(route.Template))
                    {
                        _logger.LogWarning("Route conflict detected for {Template}. Module {NewModule} conflicts with {ExistingModule}",
                            route.Template, moduleName, _routes[route.Template].ModuleName);

                        // Apply conflict resolution - last one wins for now
                        // You can implement more sophisticated strategies
                        _routes[route.Template] = route;
                    }
                    else
                    {
                        _routes[route.Template] = route;
                    }
                }

                _logger.LogInformation("Registered {Count} routes from module {ModuleName}", routes.Count, moduleName);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            OnRoutesChanged(moduleName, RouteChangeType.Added);
        }

        public void UnregisterModuleAssembly(string moduleName)
        {
            _lock.EnterWriteLock();
            try
            {
                UnregisterModuleAssemblyInternal(moduleName);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            OnRoutesChanged(moduleName, RouteChangeType.Removed);
        }

        private void UnregisterModuleAssemblyInternal(string moduleName)
        {
            if (!_moduleAssemblies.ContainsKey(moduleName))
                return;

            // Remove all routes from this module
            var routesToRemove = _routes.Where(kvp => kvp.Value.ModuleName == moduleName)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var template in routesToRemove)
            {
                _routes.Remove(template);
            }

            _moduleAssemblies.Remove(moduleName);

            _logger.LogInformation("Unregistered {Count} routes from module {ModuleName}",
                routesToRemove.Count, moduleName);
        }

        public RouteInfo? FindRoute(string path)
        {
            _lock.EnterReadLock();
            try
            {
                // Normalize path
                path = path.TrimStart('/');

                // Try exact match first
                if (_routes.TryGetValue(path, out var route))
                    return route;

                // Try pattern matching for parameterized routes
                foreach (var kvp in _routes)
                {
                    if (MatchesTemplate(path, kvp.Key))
                        return kvp.Value;
                }

                return null;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public async Task RefreshRoutesAsync()
        {
            await Task.Run(() =>
            {
                _lock.EnterWriteLock();
                try
                {
                    _routes.Clear();

                    foreach (var kvp in _moduleAssemblies)
                    {
                        var routes = DiscoverRoutes(kvp.Value, kvp.Key);
                        foreach (var route in routes)
                        {
                            _routes[route.Template] = route;
                        }
                    }
                }
                finally
                {
                    _lock.ExitWriteLock();
                }

                OnRoutesChanged("*", RouteChangeType.Refreshed);
            });
        }

        public bool HasRouteConflict(string template)
        {
            _lock.EnterReadLock();
            try
            {
                return _routes.ContainsKey(template);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private List<RouteInfo> DiscoverRoutes(Assembly assembly, string moduleName)
        {
            var routes = new List<RouteInfo>();

            try
            {
                var componentTypes = assembly.GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(ComponentBase)) && !t.IsAbstract);

                foreach (var type in componentTypes)
                {
                    var routeAttributes = type.GetCustomAttributes<RouteAttribute>();
                    foreach (var routeAttribute in routeAttributes)
                    {
                        var template = routeAttribute.Template.TrimStart('/');
                        routes.Add(new RouteInfo
                        {
                            Template = template,
                            ComponentType = type,
                            ModuleName = moduleName,
                            Assembly = assembly,
                            Priority = CalculateRoutePriority(template)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error discovering routes in assembly {Assembly}", assembly.FullName);
            }

            return routes;
        }

        private int CalculateRoutePriority(string template)
        {
            // Higher priority for more specific routes
            var priority = 0;

            // Exact routes (no parameters) have highest priority
            if (!template.Contains('{'))
                priority += 1000;

            // Longer routes have higher priority
            priority += template.Split('/').Length * 100;

            // Routes with constraints have higher priority
            if (template.Contains(':'))
                priority += 50;

            return priority;
        }

        private bool MatchesTemplate(string path, string template)
        {
            var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var templateSegments = template.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (pathSegments.Length != templateSegments.Length)
                return false;

            for (int i = 0; i < templateSegments.Length; i++)
            {
                var templateSegment = templateSegments[i];
                var pathSegment = pathSegments[i];

                // Check if it's a parameter
                if (templateSegment.StartsWith('{') && templateSegment.EndsWith('}'))
                {
                    // Parameter matches any value
                    continue;
                }

                // Exact match required
                if (!string.Equals(templateSegment, pathSegment, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private void OnRoutesChanged(string moduleName, RouteChangeType changeType)
        {
            RoutesChanged?.Invoke(this, new RouteChangedEventArgs(moduleName, changeType));
        }
    }

    public class RouteInfo
    {
        public string Template { get; set; } = string.Empty;
        public Type ComponentType { get; set; } = null!;
        public string ModuleName { get; set; } = string.Empty;
        public Assembly Assembly { get; set; } = null!;
        public int Priority { get; set; }
    }

    public class RouteChangedEventArgs : EventArgs
    {
        public string ModuleName { get; }
        public RouteChangeType ChangeType { get; }
        public DateTime Timestamp { get; }

        public RouteChangedEventArgs(string moduleName, RouteChangeType changeType)
        {
            ModuleName = moduleName;
            ChangeType = changeType;
            Timestamp = DateTime.UtcNow;
        }
    }

    public enum RouteChangeType
    {
        Added,
        Removed,
        Refreshed
    }
}