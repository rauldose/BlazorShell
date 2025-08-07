namespace BlazorShell.Core.Interfaces
{
    /// <summary>
    /// Module metadata for discovery
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ModuleMetadataAttribute : Attribute
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}