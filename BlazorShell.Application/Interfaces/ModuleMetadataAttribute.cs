namespace BlazorShell.Application.Interfaces;

[AttributeUsage(AttributeTargets.Class)]
public class ModuleMetadataAttribute : Attribute
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string Version { get; set; }
    public string Author { get; set; }
    public string Icon { get; set; }
    public string Category { get; set; }
    public int Order { get; set; }
}

