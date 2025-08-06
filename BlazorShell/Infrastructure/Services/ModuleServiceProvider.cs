// Infrastructure/Services/ModuleServiceProvider.cs
using Microsoft.Extensions.DependencyInjection;
using BlazorShell.Core.Interfaces;
using System.Collections.Concurrent;
using BlazorShell.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using BlazorShell.Core.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorShell.Infrastructure.Services
{
    public interface IModuleServiceProvider
    {
        void RegisterModuleServices(string moduleName, IServiceModule module);
        void UnregisterModuleServices(string moduleName);
        T? GetService<T>() where T : class;
        object? GetService(Type serviceType);
        IServiceProvider GetModuleServiceProvider(string moduleName);
    }

    public class ModuleServiceProvider : IModuleServiceProvider
    {
        private readonly ConcurrentDictionary<string, IServiceProvider> _moduleProviders = new();
        private readonly ConcurrentDictionary<Type, string> _serviceToModule = new();
        private readonly IServiceProvider _rootProvider;
        private readonly IServiceCollection _rootServices;
        private readonly ILogger<ModuleServiceProvider> _logger;

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
            try
            {
                // Create a new service collection for the module
                var moduleServices = new ServiceCollection();

                // IMPORTANT: Add all core services that modules might need
                // These services come from the root container
                AddCoreServicesToModule(moduleServices);

                // Let the module register its own services
                module.RegisterServices(moduleServices);

                // Build the service provider for this module
                var moduleProvider = moduleServices.BuildServiceProvider();
                _moduleProviders[moduleName] = moduleProvider;

                // Track which services belong to which module
                foreach (var descriptor in moduleServices)
                {
                    if (descriptor.ServiceType != null && !IsCoreService(descriptor.ServiceType))
                    {
                        _serviceToModule[descriptor.ServiceType] = moduleName;
                    }
                }

                _logger.LogInformation("Registered {Count} services for module {Module}",
                    moduleServices.Count, moduleName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register services for module {Module}", moduleName);
            }
        }

        private void AddCoreServicesToModule(IServiceCollection moduleServices)
        {
            // Add core framework services that modules need
            moduleServices.AddSingleton(_rootProvider.GetRequiredService<IModuleLoader>());
            moduleServices.AddSingleton(_rootProvider.GetRequiredService<IModuleRegistry>());
            moduleServices.AddSingleton(_rootProvider.GetRequiredService<IPluginAssemblyLoader>());
            moduleServices.AddSingleton(_rootProvider.GetRequiredService<IDynamicRouteService>());
            moduleServices.AddSingleton(_rootProvider.GetRequiredService<INavigationService>());
            moduleServices.AddSingleton(_rootProvider.GetRequiredService<IModuleAuthorizationService>());

            // Add database context
            moduleServices.AddScoped(sp => _rootProvider.GetRequiredService<ApplicationDbContext>());

            // Add Identity services
            moduleServices.AddScoped(sp => _rootProvider.GetRequiredService<UserManager<ApplicationUser>>());
            moduleServices.AddScoped(sp => _rootProvider.GetRequiredService<RoleManager<ApplicationRole>>());
            moduleServices.AddScoped(sp => _rootProvider.GetRequiredService<SignInManager<ApplicationUser>>());

            // Add logging
            moduleServices.AddSingleton(_rootProvider.GetRequiredService<ILoggerFactory>());
            moduleServices.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

            // Add configuration
            moduleServices.AddSingleton(_rootProvider.GetRequiredService<IConfiguration>());

            // Add HTTP context accessor
            moduleServices.AddSingleton(_rootProvider.GetRequiredService<IHttpContextAccessor>());

            // Add other essential services
            if (_rootProvider.GetService<NavigationManager>() != null)
                moduleServices.AddScoped(sp => _rootProvider.GetRequiredService<NavigationManager>());

            if (_rootProvider.GetService<IJSRuntime>() != null)
                moduleServices.AddScoped(sp => _rootProvider.GetRequiredService<IJSRuntime>());

            if (_rootProvider.GetService<AuthenticationStateProvider>() != null)
                moduleServices.AddScoped(sp => _rootProvider.GetRequiredService<AuthenticationStateProvider>());
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
                typeof(ApplicationDbContext),
                typeof(UserManager<>),
                typeof(RoleManager<>),
                typeof(SignInManager<>),
                typeof(ILogger<>),
                typeof(ILoggerFactory),
                typeof(IConfiguration),
                typeof(IHttpContextAccessor)
            };

            return coreTypes.Any(t =>
                (t.IsGenericTypeDefinition && serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == t) ||
                (!t.IsGenericTypeDefinition && t.IsAssignableFrom(serviceType)));
        }

        public void UnregisterModuleServices(string moduleName)
        {
            if (_moduleProviders.TryRemove(moduleName, out var provider))
            {
                if (provider is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                var toRemove = _serviceToModule.Where(kvp => kvp.Value == moduleName).Select(kvp => kvp.Key).ToList();
                foreach (var type in toRemove)
                {
                    _serviceToModule.TryRemove(type, out _);
                }

                _logger.LogInformation("Unregistered services for module {Module}", moduleName);
            }
        }

        public T? GetService<T>() where T : class
        {
            return GetService(typeof(T)) as T;
        }

        public object? GetService(Type serviceType)
        {
            // First try to get from module providers
            if (_serviceToModule.TryGetValue(serviceType, out var moduleName))
            {
                if (_moduleProviders.TryGetValue(moduleName, out var provider))
                {
                    var service = provider.GetService(serviceType);
                    if (service != null)
                    {
                        return service;
                    }
                }
            }

            // Fall back to root provider
            return _rootProvider.GetService(serviceType);
        }

        public IServiceProvider GetModuleServiceProvider(string moduleName)
        {
            if (_moduleProviders.TryGetValue(moduleName, out var provider))
            {
                return provider;
            }
            return _rootProvider;
        }
    }
}