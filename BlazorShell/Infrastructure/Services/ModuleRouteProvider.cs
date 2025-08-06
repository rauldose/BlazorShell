using Microsoft.AspNetCore.Components;
using System.Reflection;
using BlazorShell.Core.Interfaces;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorShell.Infrastructure.Services
{
    /// <summary>
    /// Provides dynamic route registration for modules
    /// </summary>
    public class ModuleRouteProvider
    {
        private readonly Dictionary<string, Type> _routes = new();
        private readonly ILogger<ModuleRouteProvider> _logger;

        public ModuleRouteProvider(ILogger<ModuleRouteProvider> logger)
        {
            _logger = logger;
        }

        public void RegisterModuleRoutes(string moduleName, IEnumerable<Type> componentTypes)
        {
            foreach (var componentType in componentTypes)
            {
                var routeAttribute = componentType.GetCustomAttribute<RouteAttribute>();
                if (routeAttribute != null)
                {
                    _routes[routeAttribute.Template] = componentType;
                    _logger.LogInformation("Registered route {Route} for component {Component} from module {Module}",
                        routeAttribute.Template, componentType.Name, moduleName);
                }
            }
        }

        public void UnregisterModuleRoutes(string moduleName, IEnumerable<Type> componentTypes)
        {
            foreach (var componentType in componentTypes)
            {
                var routeAttribute = componentType.GetCustomAttribute<RouteAttribute>();
                if (routeAttribute != null && _routes.ContainsKey(routeAttribute.Template))
                {
                    _routes.Remove(routeAttribute.Template);
                    _logger.LogInformation("Unregistered route {Route} for module {Module}",
                        routeAttribute.Template, moduleName);
                }
            }
        }

        public Type? GetComponentForRoute(string route)
        {
            return _routes.TryGetValue(route, out var componentType) ? componentType : null;
        }

        public Dictionary<string, Type> GetAllRoutes()
        {
            return new Dictionary<string, Type>(_routes);
        }
    }

    /// <summary>
    /// Dynamic component that renders module components
    /// </summary>
    public class ModuleDynamicComponent : ComponentBase
    {
        [Inject] private ModuleRouteProvider RouteProvider { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        [Parameter] public string Route { get; set; } = string.Empty;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var componentType = RouteProvider.GetComponentForRoute(Route);

            if (componentType != null)
            {
                builder.OpenComponent(0, componentType);
                builder.CloseComponent();
            }
            else
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "alert alert-warning");
                builder.AddContent(2, $"Component not found for route: {Route}");
                builder.CloseElement();
            }
        }
    }
}