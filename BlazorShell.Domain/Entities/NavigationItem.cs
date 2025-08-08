namespace BlazorShell.Domain.Entities;

public class NavigationItem : IAuditableEntity
{
    public int Id { get; set; }
    public int? ModuleId { get; set; }
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Url { get; set; }
    public string? Icon { get; set; }
    public int? ParentId { get; set; }
    public int Order { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? Target { get; set; }
    public string? CssClass { get; set; }
    public NavigationType Type { get; set; }

    // New: Simple flag for public access
    public bool IsPublic { get; set; } = false;

    // New: Minimum required role (optional - for simple role-based access)
    public string? MinimumRole { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }

    // Navigation properties
    public virtual Module? Module { get; set; }
    public virtual NavigationItem? Parent { get; set; }
    public virtual ICollection<NavigationItem> Children { get; set; } = new HashSet<NavigationItem>();
    public virtual ICollection<PagePermission> PagePermissions { get; set; } = new HashSet<PagePermission>();
}
