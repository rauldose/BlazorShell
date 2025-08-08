namespace BlazorShell.Infrastructure.Configuration;

public class ModulesConfiguration
{
    public ModuleSettings? ModuleSettings { get; set; }
    public List<ModuleConfig> Modules { get; set; } = new();
}
