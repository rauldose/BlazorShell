using BlazorShell.Core.Interfaces;

namespace BlazorShell.Web.Adapters
{
    /// <summary>
    /// Adapter to bridge the existing DynamicRouteService implementation with the Core interface
    /// </summary>
    public class DynamicRouteServiceAdapter : IDynamicRouteService
    {
        private readonly BlazorShell.Infrastructure.Services.DynamicRouteService _innerService;

        public DynamicRouteServiceAdapter(BlazorShell.Infrastructure.Services.DynamicRouteService innerService)
        {
            _innerService = innerService;
        }

        public void RegisterRoutes(IEnumerable<RouteInfo> routes)
        {
            // For now, this is a simplified implementation
            // The actual registration would need to be adapted based on the inner service
        }

        public void UnregisterRoutes(string moduleName)
        {
            _innerService.UnregisterModuleAssembly(moduleName);
        }

        public IEnumerable<RouteInfo> GetRoutes()
        {
            // Convert from the inner service's format to RouteInfo
            return _innerService.Routes.Values.Select(route => new RouteInfo
            {
                Path = route.Template,
                ComponentType = route.ComponentType,
                ModuleName = route.ModuleName
            });
        }

        public RouteInfo? GetRoute(string path)
        {
            var route = _innerService.FindRoute(path);
            if (route == null) return null;

            return new RouteInfo
            {
                Path = route.Template,
                ComponentType = route.ComponentType,
                ModuleName = route.ModuleName
            };
        }
    }
}