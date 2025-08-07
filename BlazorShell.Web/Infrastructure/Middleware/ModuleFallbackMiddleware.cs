// Infrastructure/Middleware/ModuleFallbackMiddleware.cs
using BlazorShell.Core.Interfaces;
using BlazorShell.Infrastructure.Services;

namespace BlazorShell.Infrastructure.Middleware
{
    public class ModuleFallbackMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ModuleFallbackMiddleware> _logger;

        public ModuleFallbackMiddleware(RequestDelegate next, ILogger<ModuleFallbackMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IDynamicRouteService dynamicRouteService, IModuleLoader moduleLoader)
        {
            // Check if this is a navigation request (not API or static files)
            if (context.Request.Path.HasValue &&
                !context.Request.Path.Value.StartsWith("/_") &&
                !context.Request.Path.Value.StartsWith("/api") &&
                !context.Request.Path.Value.Contains("."))
            {
                var path = context.Request.Path.Value.TrimStart('/');

                // Check if this matches a dynamic route
                var routeInfo = dynamicRouteService.FindRoute(path);
                if (routeInfo != null)
                {
                    _logger.LogDebug("Module route found for path: {Path}", path);

                    // Ensure modules are initialized
                    await moduleLoader.InitializeModulesAsync();

                    // The route exists, let Blazor handle it
                    //context.Request.Path = "/_blazor";
                }
            }

            await _next(context);
        }
    }

    public static class ModuleFallbackMiddlewareExtensions
    {
        public static IApplicationBuilder UseModuleFallback(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ModuleFallbackMiddleware>();
        }
    }
}