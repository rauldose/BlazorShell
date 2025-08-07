using Autofac;
using BlazorShell.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorShell.Infrastructure.Services
{
    /// <summary>
    /// Manages dynamic service registration for modules
    /// </summary>
    public class ModuleServiceManager
    {
        private readonly ILifetimeScope _rootScope;
        private readonly Dictionary<string, ILifetimeScope> _moduleScopes;
        private readonly ILogger<ModuleServiceManager> _logger;

        public ModuleServiceManager(ILifetimeScope rootScope, ILogger<ModuleServiceManager> logger)
        {
            _rootScope = rootScope;
            _logger = logger;
            _moduleScopes = new Dictionary<string, ILifetimeScope>();
        }

        public IServiceProvider RegisterModuleServices(string moduleName, IServiceModule module)
        {
            try
            {
                // Create a child lifetime scope for the module
                var moduleScope = _rootScope.BeginLifetimeScope(moduleName, builder =>
                {
                    // Create a temporary service collection
                    var services = new ServiceCollection();

                    // Let the module register its services
                    module.RegisterServices(services);

                    // Register each service with Autofac
                    foreach (var descriptor in services)
                    {
                        if (descriptor.ImplementationType != null)
                        {
                            var registration = builder.RegisterType(descriptor.ImplementationType);

                            if (descriptor.ServiceType != null)
                            {
                                registration = registration.As(descriptor.ServiceType);
                            }

                            // Set lifetime
                            switch (descriptor.Lifetime)
                            {
                                case ServiceLifetime.Singleton:
                                    registration.SingleInstance();
                                    break;
                                case ServiceLifetime.Scoped:
                                    registration.InstancePerLifetimeScope();
                                    break;
                                case ServiceLifetime.Transient:
                                    registration.InstancePerDependency();
                                    break;
                            }
                        }
                        else if (descriptor.ImplementationInstance != null)
                        {
                            builder.RegisterInstance(descriptor.ImplementationInstance)
                                .As(descriptor.ServiceType);
                        }
                        else if (descriptor.ImplementationFactory != null)
                        {
                            builder.Register(c => descriptor.ImplementationFactory(c.Resolve<IServiceProvider>()))
                                .As(descriptor.ServiceType);
                        }
                    }
                });

                _moduleScopes[moduleName] = moduleScope;
                _logger.LogInformation("Registered services for module {Module}", moduleName);

                return moduleScope.Resolve<IServiceProvider>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register services for module {Module}", moduleName);
                throw;
            }
        }

        public void UnregisterModuleServices(string moduleName)
        {
            if (_moduleScopes.TryGetValue(moduleName, out var scope))
            {
                scope.Dispose();
                _moduleScopes.Remove(moduleName);
                _logger.LogInformation("Unregistered services for module {Module}", moduleName);
            }
        }

        public IServiceProvider? GetModuleServiceProvider(string moduleName)
        {
            return _moduleScopes.TryGetValue(moduleName, out var scope)
                ? scope.Resolve<IServiceProvider>()
                : null;
        }
    }
}