using Microsoft.AspNetCore.Identity;

namespace BlazorShell.Core.Entities
{
    // Enhanced Role entity
    public class ApplicationRole : IdentityRole
    {
        public string? Description { get; set; }
        public bool IsSystemRole { get; set; }
        public DateTime CreatedDate { get; set; }

        // Navigation properties
        public virtual ICollection<ModulePermission> ModulePermissions { get; set; } = new List<ModulePermission>();
    }
}