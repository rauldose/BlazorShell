using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BlazorShell.Application.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorShell.ModuleSDK.Base
{
    public abstract class ModuleBase : IModule
    {
        protected ILogger? Logger { get; private set; }
        protected IServiceProvider? ServiceProvider { get; private set; }

        public abstract string Name { get; }
        public abstract string DisplayName { get; }
        public abstract string Description { get; }
        public virtual string Version => "1.0.0";
        public virtual string Author => "Unknown";
        public virtual string Icon => "bi bi-puzzle";
        public virtual string Category => "General";
        public virtual int Order => 100;

        public virtual async Task<bool> InitializeAsync(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            Logger = serviceProvider.GetService<ILogger>();
            Logger?.LogInformation($"Initializing module: {Name}");
            return await OnInitializeAsync();
        }

        public virtual async Task<bool> ActivateAsync()
        {
            Logger?.LogInformation($"Activating module: {Name}");
            return await OnActivateAsync();
        }

        public virtual async Task<bool> DeactivateAsync()
        {
            Logger?.LogInformation($"Deactivating module: {Name}");
            return await OnDeactivateAsync();
        }

        public virtual IEnumerable<NavigationItem> GetNavigationItems()
        {
            return Array.Empty<NavigationItem>();
        }

        public virtual IEnumerable<Type> GetComponentTypes()
        {
            return GetType().Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(ComponentBase)) && !t.IsAbstract);
        }

        public virtual Dictionary<string, object> GetDefaultSettings()
        {
            return new Dictionary<string, object>();
        }

        protected virtual Task<bool> OnInitializeAsync() => Task.FromResult(true);
        protected virtual Task<bool> OnActivateAsync() => Task.FromResult(true);
        protected virtual Task<bool> OnDeactivateAsync() => Task.FromResult(true);

        IEnumerable<Domain.Entities.NavigationItem> IModule.GetNavigationItems()
        {
            throw new NotImplementedException();
        }
    }
    
    public class NavigationItem
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}
