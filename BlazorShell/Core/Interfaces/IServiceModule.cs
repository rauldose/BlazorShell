using Microsoft.Extensions.DependencyInjection;

namespace BlazorShell.Core.Interfaces
{
    /// <summary>
    /// Interface for modules that provide services
    /// </summary>
    public interface IServiceModule : IModule
    {
        void RegisterServices(IServiceCollection services);
    }
}