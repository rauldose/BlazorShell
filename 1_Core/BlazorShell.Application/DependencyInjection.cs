using Microsoft.Extensions.DependencyInjection;
using BlazorShell.Application.Services;

namespace BlazorShell.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<AuthenticationService>();
    
            return services;
        }
    }
}
