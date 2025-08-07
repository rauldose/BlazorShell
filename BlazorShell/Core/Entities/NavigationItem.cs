using BlazorShell.Core.Interfaces;
using BlazorShell.Core.Enums;

namespace BlazorShell.Core.Entities
{
    // Dynamic navigation system
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
        public bool IsVisible { get; set; }
        public string? RequiredPermission { get; set; }
        public string? RequiredRole { get; set; }
        public string? Target { get; set; } // _blank, _self, etc.
        public string? CssClass { get; set; }
        public NavigationType Type { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation properties
        public virtual Module? Module { get; set; }
        public virtual NavigationItem? Parent { get; set; }
        public virtual ICollection<NavigationItem> Children { get; set; } = new List<NavigationItem>();
    }
}