using Microsoft.AspNetCore.Builder;

namespace BlazorShell.Infrastructure.Middleware;

public static class ModuleFallbackMiddlewareExtensions
{
    public static IApplicationBuilder UseModuleFallback(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ModuleFallbackMiddleware>();
    }
}
