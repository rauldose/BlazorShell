using System.Collections.Concurrent;
using System.Reflection;
using BlazorShell.Application.Services;

namespace BlazorShell.Infrastructure.Services.Implementations
{
    public class RouteAssemblyProvider : IRouteAssemblyProvider
    {
        private readonly ConcurrentDictionary<Assembly, byte> _assemblies = new();

        public IEnumerable<Assembly> Assemblies => _assemblies.Keys;

        public void AddAssembly(Assembly assembly)
            => _assemblies.TryAdd(assembly, 0);

        public void RemoveAssembly(Assembly assembly)
            => _assemblies.TryRemove(assembly, out _);
    }
}
