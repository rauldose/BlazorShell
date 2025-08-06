using Microsoft.Extensions.DependencyInjection;
using BlazorShell.Application.Interfaces;
using BlazorShell.ModuleSystem.Services;
using BlazorShell.Infrastructure.Services;

namespace BlazorShell.ModuleSystem
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddModuleSystem(this IServiceCollection services)
        {
            services.AddSingleton<IModuleLoader, ModuleLoader>();
            services.AddSingleton<IModuleRegistry, ModuleRegistry>();
            services.AddSingleton<IPluginAssemblyLoader, PluginAssemblyLoader>();
            services.AddSingleton<IDynamicRouteService, DynamicRouteService>();
            services.AddSingleton<IModuleServiceProvider, ModuleServiceProvider>();
            services.AddScoped<ModuleCleanupService>();
            return services;
        }
    }
}
