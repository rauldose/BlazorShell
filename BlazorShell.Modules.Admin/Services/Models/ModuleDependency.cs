namespace BlazorShell.Modules.Admin.Services.Models;

public class ModuleDependency
{
    public string ModuleName { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsLoaded { get; set; }
    public bool IsSatisfied { get; set; }
}
