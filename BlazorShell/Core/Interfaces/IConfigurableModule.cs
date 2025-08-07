namespace BlazorShell.Core.Interfaces
{
    /// <summary>
    /// Interface for modules with configuration requirements
    /// </summary>
    public interface IConfigurableModule : IModule
    {
        Type ConfigurationComponentType { get; }
        Task<bool> ValidateConfigurationAsync(Dictionary<string, object> configuration);
        Task ApplyConfigurationAsync(Dictionary<string, object> configuration);
    }
}