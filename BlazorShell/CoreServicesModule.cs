using Autofac;
using BlazorShell.Application.Interfaces;
using BlazorShell.Application.Services;
using BlazorShell.Infrastructure.Security;
using BlazorShell.Infrastructure.Services;
using BlazorShell.Infrastructure.Services.Implementations;
using Microsoft.Extensions.Hosting;

public class CoreServicesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register core services with proper lifetimes
        builder.RegisterType<ModuleLoader>().As<IModuleLoader>().SingleInstance();
        builder.RegisterType<ModuleRegistry>().As<IModuleRegistry>().SingleInstance();
        builder.RegisterType<NavigationService>().As<INavigationService>().SingleInstance();
        builder.RegisterType<StateContainer>().As<IStateContainer>().InstancePerLifetimeScope();
        builder.RegisterType<UnifiedAuthorizationService>().As<IModuleAuthorizationService>().InstancePerLifetimeScope();
        builder.RegisterType<UnifiedAuthorizationService>().As<IPageAuthorizationService>().InstancePerLifetimeScope();
        builder.RegisterType<PluginAssemblyLoader>().As<IPluginAssemblyLoader>().SingleInstance();
        builder.RegisterType<ModuleRouteProvider>().AsSelf().SingleInstance();
        builder.RegisterType<ModuleMetadataCache>().AsSelf().SingleInstance();
        builder.RegisterType<RouteAssemblyProvider>().As<IRouteAssemblyProvider>().SingleInstance();

        builder.RegisterType<DynamicRouteService>()
            .As<IDynamicRouteService>()
            .SingleInstance();
        builder.RegisterType<LazyModuleLoader>()
            .As<ILazyModuleLoader>()
            .SingleInstance();

        builder.RegisterType<ModuleHotReloadService>()
            .As<IModuleHotReloadService>()
            .As<IHostedService>()
            .SingleInstance();
        builder.RegisterType<ModulePerformanceMonitor>()
            .As<IModulePerformanceMonitor>()
            .SingleInstance();
        builder.RegisterType<ModuleCleanupService>()
            .As<IHostedService>()
            .SingleInstance();
        // ModuleServiceProvider registered elsewhere with access to serviceCollection
    }
}

