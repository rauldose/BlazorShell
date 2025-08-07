using BlazorShell.Core.Interfaces;

namespace BlazorShell.Core.Entities;

// Module configuration with versioning and dependencies
public class Module : IAuditableEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Version { get; set; }
    public string? Author { get; set; }
    public string? AssemblyName { get; set; }
    public string? EntryType { get; set; }
    public string? Icon { get; set; }
    public string? Category { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsCore { get; set; }
    public int LoadOrder { get; set; }
    public string? Dependencies { get; set; } // JSON serialized list
    public string? Configuration { get; set; } // JSON configuration
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }

    // Navigation properties
    public virtual ICollection<ModulePermission> Permissions { get; set; } = new List<ModulePermission>();
    public virtual ICollection<NavigationItem> NavigationItems { get; set; } = new List<NavigationItem>();
}