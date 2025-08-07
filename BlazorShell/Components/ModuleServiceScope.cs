// Components/ModuleServiceScope.cs
using Microsoft.AspNetCore.Components;
using BlazorShell.Core.Services;

namespace BlazorShell.Components
{
    /// <summary>
    /// Provides a service scope for module components
    /// </summary>
    public class ModuleServiceScope : IServiceProvider
    {
        private readonly IServiceProvider _rootProvider;
        private readonly IModuleServiceProvider _moduleServiceProvider;
        private readonly string? _moduleName;

        public ModuleServiceScope(
            IServiceProvider rootProvider,
            IModuleServiceProvider moduleServiceProvider,
            string? moduleName = null)
        {
            _rootProvider = rootProvider;
            _moduleServiceProvider = moduleServiceProvider;
            _moduleName = moduleName;
        }

        public object? GetService(Type serviceType)
        {
            // Try module services first
            var service = _moduleServiceProvider.GetService(serviceType);
            if (service != null)
                return service;

            // Fall back to root provider
            return _rootProvider.GetService(serviceType);
        }
    }
}