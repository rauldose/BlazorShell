// BlazorShell.Infrastructure/Services/ModuleRouteProvider.cs
using Microsoft.AspNetCore.Components;
using System.Reflection;
using BlazorShell.Application.Interfaces;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Infrastructure.Services
{
    /// <summary>
    /// Provides dynamic route registration for modules with support for multiple routes per component
    /// </summary>
    public class ModuleRouteProvider
    {
        private readonly Dictionary<string, Type> _routes = new();
        private readonly Dictionary<Type, List<string>> _componentRoutes = new();
        private readonly ILogger<ModuleRouteProvider> _logger;

        public ModuleRouteProvider(ILogger<ModuleRouteProvider> logger)
        {
            _logger = logger;
        }

        public void RegisterModuleRoutes(string moduleName, IEnumerable<Type> componentTypes)
        {
            foreach (var componentType in componentTypes)
            {
                // Fixed: Get all route attributes (plural) to handle multiple @page directives
                var routeAttributes = componentType.GetCustomAttributes<RouteAttribute>().ToList();

                if (routeAttributes.Any())
                {
                    // Store all routes for this component
                    _componentRoutes[componentType] = new List<string>();

                    foreach (var routeAttribute in routeAttributes)
                    {
                        _routes[routeAttribute.Template] = componentType;
                        _componentRoutes[componentType].Add(routeAttribute.Template);

                        _logger.LogInformation("Registered route {Route} for component {Component} from module {Module}",
                            routeAttribute.Template, componentType.Name, moduleName);
                    }
                }
            }
        }

        public void UnregisterModuleRoutes(string moduleName, IEnumerable<Type> componentTypes)
        {
            foreach (var componentType in componentTypes)
            {
                // Get all routes for this component
                if (_componentRoutes.TryGetValue(componentType, out var routes))
                {
                    foreach (var route in routes)
                    {
                        if (_routes.ContainsKey(route))
                        {
                            _routes.Remove(route);
                            _logger.LogInformation("Unregistered route {Route} for module {Module}",
                                route, moduleName);
                        }
                    }

                    _componentRoutes.Remove(componentType);
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

        public List<string> GetRoutesForComponent(Type componentType)
        {
            return _componentRoutes.TryGetValue(componentType, out var routes)
                ? new List<string>(routes)
                : new List<string>();
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