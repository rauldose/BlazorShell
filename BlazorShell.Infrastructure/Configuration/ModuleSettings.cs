namespace BlazorShell.Infrastructure.Configuration;

public class ModuleSettings
{
    public bool EnableDynamicLoading { get; set; }
    public string? ModulesPath { get; set; }
    public bool AllowRemoteModules { get; set; }
    public bool AutoLoadOnStartup { get; set; }
    public bool CacheModuleMetadata { get; set; }
}
