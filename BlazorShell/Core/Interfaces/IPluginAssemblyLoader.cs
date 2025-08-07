using System.Reflection;

namespace BlazorShell.Core.Interfaces
{
    /// <summary>
    /// Plugin assembly loader
    /// </summary>
    public interface IPluginAssemblyLoader
    {
        Assembly LoadPlugin(string path);
        void UnloadPlugin(string pluginName);
        IEnumerable<Type> GetTypesFromAssembly(Assembly assembly, Type interfaceType);
        T CreateInstance<T>(Type type) where T : class;
    }
}