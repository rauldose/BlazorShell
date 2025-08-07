using Autofac;
using BlazorShell.Core.Interfaces;
using BlazorShell.Core.Services;
using BlazorShell.Infrastructure.Services;
using BlazorShell.Infrastructure.Security;

namespace BlazorShell.Infrastructure.Startup;

// Module registration for Autofac - NO CONSTRUCTOR PARAMETERS
public class CoreServicesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register core services with proper lifetimes
        builder.RegisterType<ModuleLoader>().As<IModuleLoader>().SingleInstance();
        builder.RegisterType<ModuleRegistry>().As<IModuleRegistry>().SingleInstance();
        builder.RegisterType<NavigationService>().As<INavigationService>().SingleInstance();
        builder.RegisterType<StateContainer>().As<IStateContainer>().InstancePerLifetimeScope();
        builder.RegisterType<ModuleAuthorizationService>().As<IModuleAuthorizationService>().InstancePerLifetimeScope();
        builder.RegisterType<PluginAssemblyLoader>().As<IPluginAssemblyLoader>().SingleInstance();
        builder.RegisterType<ModuleRouteProvider>().AsSelf().SingleInstance();
        builder.RegisterType<ModuleServiceManager>().AsSelf().SingleInstance();
        builder.RegisterType<ModuleMetadataCache>().AsSelf().SingleInstance();

        builder.RegisterType<DynamicRouteService>()
            .As<IDynamicRouteService>()
            .SingleInstance();
        builder.RegisterType<LazyModuleLoader>()
            .As<ILazyModuleLoader>()
            .SingleInstance();

        builder.RegisterType<ModuleHotReloadService>()
            .As<IModuleHotReloadService>()
            .As<IHostedService>()  // Register as hosted service for auto-start
            .SingleInstance();
        builder.RegisterType<ModulePerformanceMonitor>()
            .As<IModulePerformanceMonitor>()
            .SingleInstance();
        builder.RegisterType<ModuleCleanupService>()
            .As<IHostedService>()
            .SingleInstance();
        // Note: ModuleServiceProvider is registered in the ConfigureContainer method above
        // because it needs access to the serviceCollection variable
    }
}

public class InfrastructureModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register infrastructure services
        builder.RegisterType<EmailService>().As<IEmailService>().InstancePerLifetimeScope();
        builder.RegisterType<FileStorageService>().As<IFileStorageService>().InstancePerLifetimeScope();
        builder.RegisterType<CacheService>().As<ICacheService>().SingleInstance();
    }
}