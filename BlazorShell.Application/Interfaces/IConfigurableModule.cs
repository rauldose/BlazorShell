namespace BlazorShell.Application.Interfaces;

public interface IConfigurableModule : IModule
{
    Type ConfigurationComponentType { get; }
    Task<bool> ValidateConfigurationAsync(Dictionary<string, object> configuration);
    Task ApplyConfigurationAsync(Dictionary<string, object> configuration);
}

