using System.Reflection;
namespace BlazorShell.Application.Interfaces;

public interface IPluginAssemblyLoader
{
    Assembly LoadPlugin(string path);
    void UnloadPlugin(string pluginName);
    IEnumerable<Type> GetTypesFromAssembly(Assembly assembly, Type interfaceType);
    T CreateInstance<T>(Type type) where T : class;
}

