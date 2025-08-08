using System.Collections.Generic;

namespace BlazorShell.Application.Configuration;

public class ModulesConfiguration
{
    public ModuleSettings? ModuleSettings { get; set; }
    public List<ModuleConfig>? Modules { get; set; }
}
