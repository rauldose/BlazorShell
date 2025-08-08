using System.Collections.Generic;
using System.Reflection;

namespace BlazorShell.Application.Services
{
    public interface IRouteAssemblyProvider
    {
        IEnumerable<Assembly> Assemblies { get; }
        void AddAssembly(Assembly assembly);
        void RemoveAssembly(Assembly assembly);
    }
}
