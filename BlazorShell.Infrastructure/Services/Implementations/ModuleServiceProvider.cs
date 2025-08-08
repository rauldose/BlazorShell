// Infrastructure/Services/ImprovedModuleServiceProvider.cs
using Microsoft.Extensions.DependencyInjection;
using BlazorShell.Application.Interfaces;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using BlazorShell.Application.Services;
using BlazorShell.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using BlazorShell.Domain.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using BlazorShell.Application.Interfaces.Repositories;
using BlazorShell.Infrastructure.Repositories;
using BlazorShell.Domain.Events;
using Microsoft.Extensions.Caching.Memory;

namespace BlazorShell.Infrastructure.Services
{

    public class ModuleServiceProvider : IModuleServiceProvider
    {
        private readonly ConcurrentDictionary<string, ModuleServiceContainer> _moduleContainers = new();
        private readonly ConcurrentDictionary<Type, string> _serviceToModule = new();
        private readonly IServiceProvider _rootProvider;
        private readonly IServiceCollection _rootServices;
        private readonly ILogger<ModuleServiceProvider> _logger;
        private readonly object _registrationLock = new object();

        public ModuleServiceProvider(
            IServiceProvider rootProvider,
            IServiceCollection rootServices,
            ILogger<ModuleServiceProvider> logger)
        {
            _rootProvider = rootProvider;
            _rootServices = rootServices;
            _logger = logger;
        }

        public void RegisterModuleServices(string moduleName, IServiceModule module)
        {
            lock (_registrationLock)
            {
                try
                {
                    _logger.LogInformation("Registering services for module {Module}", moduleName);

                    // Check if module is already registered
                    if (_moduleContainers.ContainsKey(moduleName))
                    {
                        _logger.LogWarning("Module {Module} is already registered. Refreshing services.", moduleName);
                        RefreshModuleServices(moduleName, module);
                        return;
                    }

                    // Create a new service collection for the module
                    var moduleServices = new ServiceCollection();

                    // Add all core services that modules might need
                    AddCoreServicesToModule(moduleServices);

                    // Let the module register its own services
                    module.RegisterServices(moduleServices);

                    // Build the service provider for this module
                    var moduleProvider = moduleServices.BuildServiceProvider();

                    // Store the container
                    var container = new ModuleServiceContainer
                    {
                        ServiceProvider = moduleProvider,
                        ServiceCollection = moduleServices,
                        RegisteredTypes = new HashSet<Type>()
                    };

                    // Track which services belong to which module
                    foreach (var descriptor in moduleServices)
                    {
                        if (descriptor.ServiceType != null && !IsCoreService(descriptor.ServiceType))
                        {
                            _serviceToModule[descriptor.ServiceType] = moduleName;
                            container.RegisteredTypes.Add(descriptor.ServiceType);
                        }
                    }

                    _moduleContainers[moduleName] = container;

                    _logger.LogInformation("Successfully registered {Count} services for module {Module}",
                        container.RegisteredTypes.Count, moduleName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to register services for module {Module}", moduleName);
                    throw;
                }
            }
        }

        public void RefreshModuleServices(string moduleName, IServiceModule module)
        {
            lock (_registrationLock)
            {
                try
                {
                    _logger.LogInformation("Refreshing services for module {Module}", moduleName);

                    // Remove old registrations
                    if (_moduleContainers.TryRemove(moduleName, out var oldContainer))
                    {
                        // Clean up old service type mappings
                        foreach (var type in oldContainer.RegisteredTypes)
                        {
                            _serviceToModule.TryRemove(type, out _);
                        }

                        // Dispose old provider if disposable
                        if (oldContainer.ServiceProvider is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }

                    // Re-register the module
                    RegisterModuleServicesInternal(moduleName, module);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to refresh services for module {Module}", moduleName);
                }
            }
        }

        private void RegisterModuleServicesInternal(string moduleName, IServiceModule module)
        {
            // Create a new service collection for the module
            var moduleServices = new ServiceCollection();

            // Add all core services that modules might need
            AddCoreServicesToModule(moduleServices);

            // Let the module register its own services
            module.RegisterServices(moduleServices);

            // Build the service provider for this module
            var moduleProvider = moduleServices.BuildServiceProvider();

            // Store the container
            var container = new ModuleServiceContainer
            {
                ServiceProvider = moduleProvider,
                ServiceCollection = moduleServices,
                RegisteredTypes = new HashSet<Type>()
            };

            // Track which services belong to which module
            foreach (var descriptor in moduleServices)
            {
                if (descriptor.ServiceType != null && !IsCoreService(descriptor.ServiceType))
                {
                    _serviceToModule[descriptor.ServiceType] = moduleName;
                    container.RegisteredTypes.Add(descriptor.ServiceType);
                }
            }

            _moduleContainers[moduleName] = container;
        }

        private void AddCoreServicesToModule(IServiceCollection moduleServices)
        {
            // Add core framework services that modules need

            // Use factory methods to ensure we get the current instances from root provider
            moduleServices.AddSingleton(sp => _rootProvider.GetRequiredService<IModuleLoader>());
            moduleServices.AddSingleton(sp => _rootProvider.GetRequiredService<IModuleRegistry>());
            moduleServices.AddSingleton(sp => _rootProvider.GetRequiredService<IPluginAssemblyLoader>());
            moduleServices.AddSingleton(sp => _rootProvider.GetRequiredService<IDynamicRouteService>());
            moduleServices.AddSingleton(sp => _rootProvider.GetRequiredService<INavigationService>());
            moduleServices.AddSingleton(sp => _rootProvider.GetRequiredService<IModuleAuthorizationService>());
            moduleServices.AddSingleton(sp => _rootProvider.GetRequiredService<IStateContainer>());
            moduleServices.AddSingleton(sp => _rootProvider.GetRequiredService<IMemoryCache>());
            moduleServices.AddSingleton(sp => _rootProvider.GetRequiredService<ISettingsService>());

            // Add database context as scoped
            moduleServices.AddScoped(sp => _rootProvider.GetRequiredService<ApplicationDbContext>());

            // Add Identity services as scoped
            moduleServices.AddScoped(sp => _rootProvider.GetRequiredService<UserManager<ApplicationUser>>());
            moduleServices.AddScoped(sp => _rootProvider.GetRequiredService<RoleManager<ApplicationRole>>());
            moduleServices.AddScoped(sp => _rootProvider.GetRequiredService<SignInManager<ApplicationUser>>());

            moduleServices.AddScoped(sp => _rootProvider.GetRequiredService<IModuleRepository>());
            moduleServices.AddScoped(sp => _rootProvider.GetRequiredService<IUserRepository>());
            moduleServices.AddScoped(sp => _rootProvider.GetRequiredService<IAuditLogRepository>());
            moduleServices.AddScoped(sp => _rootProvider.GetRequiredService<IDomainEventDispatcher>());
            // Add logging
            moduleServices.AddSingleton(_rootProvider.GetRequiredService<ILoggerFactory>());
            moduleServices.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

            // Add configuration
            moduleServices.AddSingleton(_rootProvider.GetRequiredService<IConfiguration>());

            // Add HTTP context accessor
            moduleServices.AddSingleton(sp => _rootProvider.GetService<IHttpContextAccessor>());

            // Add Blazor services if available
            var navigationManager = _rootProvider.GetService<NavigationManager>();
            if (navigationManager != null)
            {
                moduleServices.AddScoped(sp => navigationManager);
            }

            var jsRuntime = _rootProvider.GetService<IJSRuntime>();
            if (jsRuntime != null)
            {
                moduleServices.AddScoped(sp => jsRuntime);
            }

            var authStateProvider = _rootProvider.GetService<AuthenticationStateProvider>();
            if (authStateProvider != null)
            {
                moduleServices.AddScoped(sp => authStateProvider);
            }

            // Add self reference so modules can access the module service provider
            moduleServices.AddSingleton<IModuleServiceProvider>(this);
        }

        private bool IsCoreService(Type serviceType)
        {
            // List of core service types that come from the root container
            var coreTypes = new[]
            {
                typeof(IModuleLoader),
                typeof(IModuleRegistry),
                typeof(IPluginAssemblyLoader),
                typeof(IDynamicRouteService),
                typeof(INavigationService),
                typeof(IModuleAuthorizationService),
                typeof(IStateContainer),
                typeof(ApplicationDbContext),
                typeof(UserManager<>),
                typeof(RoleManager<>),
                typeof(SignInManager<>),
                typeof(ILogger<>),
                typeof(ILoggerFactory),
                typeof(IUserRepository),
                typeof(IAuditLogRepository),
                typeof(IModuleRepository),
                typeof(IDomainEventDispatcher),
                typeof(IConfiguration),
                typeof(IHttpContextAccessor),
                typeof(ISettingsService),
                typeof(IMemoryCache),
                typeof(NavigationManager),
                typeof(IJSRuntime),
                typeof(AuthenticationStateProvider),
                typeof(IModuleServiceProvider)
            };

            return coreTypes.Any(t =>
                (t.IsGenericTypeDefinition && serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == t) ||
                (!t.IsGenericTypeDefinition && t.IsAssignableFrom(serviceType)));
        }

        public void UnregisterModuleServices(string moduleName)
        {
            lock (_registrationLock)
            {
                try
                {
                    if (_moduleContainers.TryRemove(moduleName, out var container))
                    {
                        // Remove service type mappings
                        foreach (var type in container.RegisteredTypes)
                        {
                            _serviceToModule.TryRemove(type, out _);
                        }

                        // Dispose the service provider
                        if (container.ServiceProvider is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }

                        _logger.LogInformation("Unregistered services for module {Module}", moduleName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error unregistering services for module {Module}", moduleName);
                }
            }
        }

        public T? GetService<T>() where T : class
        {
            return GetService(typeof(T)) as T;
        }

        public object? GetService(Type serviceType)
        {
            try
            {
                // First try to get from module providers
                if (_serviceToModule.TryGetValue(serviceType, out var moduleName))
                {
                    if (_moduleContainers.TryGetValue(moduleName, out var container))
                    {
                        var service = container.ServiceProvider.GetService(serviceType);
                        if (service != null)
                        {
                            return service;
                        }
                    }
                }

                // Try all module containers (in case service wasn't tracked)
                foreach (var container in _moduleContainers.Values)
                {
                    var service = container.ServiceProvider.GetService(serviceType);
                    if (service != null)
                    {
                        return service;
                    }
                }

                // Fall back to root provider
                return _rootProvider.GetService(serviceType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service of type {ServiceType}", serviceType.Name);
                return null;
            }
        }

        public IServiceProvider GetModuleServiceProvider(string moduleName)
        {
            if (_moduleContainers.TryGetValue(moduleName, out var container))
            {
                return container.ServiceProvider;
            }

            _logger.LogWarning("Module {Module} not found, returning root provider", moduleName);
            return _rootProvider;
        }

        public bool IsModuleRegistered(string moduleName)
        {
            return _moduleContainers.ContainsKey(moduleName);
        }

        public IEnumerable<string> GetRegisteredModules()
        {
            return _moduleContainers.Keys.ToList();
        }

        private class ModuleServiceContainer
        {
            public IServiceProvider ServiceProvider { get; set; } = null!;
            public IServiceCollection ServiceCollection { get; set; } = null!;
            public HashSet<Type> RegisteredTypes { get; set; } = new();
        }
    }
}