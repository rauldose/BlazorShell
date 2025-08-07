using Microsoft.Extensions.DependencyInjection;

namespace BlazorShell.Application.Interfaces;

public interface IServiceModule : IModule
{
    void RegisterServices(IServiceCollection services);
}

