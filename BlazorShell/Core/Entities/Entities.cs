
using Microsoft.AspNetCore.Identity;

namespace BlazorShell.Core.Entities
{
    // Enhanced User entity
    public class ApplicationUser : IdentityUser, IAuditableEntity
    {
        public string? FullName { get; set; }
        public string? ProfilePicture { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginDate { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation properties
        public virtual ICollection<ModulePermission> ModulePermissions { get; set; }
        public virtual ICollection<AuditLog> AuditLogs { get; set; }
    }

    // Enhanced Role entity
    public class ApplicationRole : IdentityRole
    {
        public string? Description { get; set; }
        public bool IsSystemRole { get; set; }
        public DateTime CreatedDate { get; set; }

        // Navigation properties
        public virtual ICollection<ModulePermission> ModulePermissions { get; set; }
    }

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
        public virtual ICollection<ModulePermission> Permissions { get; set; }
        public virtual ICollection<NavigationItem> NavigationItems { get; set; }
    }

    // Granular permission system
    public class ModulePermission
    {
        public int Id { get; set; }
        public int ModuleId { get; set; }
        public string? UserId { get; set; }
        public string? RoleId { get; set; }
        public string? PermissionType { get; set; } // Read, Write, Delete, Execute
        public bool IsGranted { get; set; }
        public DateTime GrantedDate { get; set; }
        public string? GrantedBy { get; set; }

        // Navigation properties
        public virtual Module Module { get; set; }
        public virtual ApplicationUser User { get; set; }
        public virtual ApplicationRole Role { get; set; }
    }

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
        public virtual Module Module { get; set; }
        public virtual NavigationItem Parent { get; set; }
        public virtual ICollection<NavigationItem> Children { get; set; }
    }

    // Comprehensive audit logging
    public class AuditLog
    {
        public long Id { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Action { get; set; }
        public string? EntityName { get; set; }
        public string? EntityId { get; set; }
        public string? OldValues { get; set; } // JSON
        public string? NewValues { get; set; } // JSON
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedDate { get; set; }

        // Navigation properties
        public virtual ApplicationUser User { get; set; }
    }

    // Application settings
    public class Setting
    {
        public int Id { get; set; }
        public string? Category { get; set; }
        public string? Key { get; set; }
        public string? Value { get; set; }
        public string? DataType { get; set; }
        public string? Description { get; set; }
        public bool IsPublic { get; set; }
        public bool IsEncrypted { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }

    // Interfaces
    public interface IAuditableEntity
    {
        DateTime CreatedDate { get; set; }
        DateTime? ModifiedDate { get; set; }
        string? CreatedBy { get; set; }
        string? ModifiedBy { get; set; }
    }

    // Enums
    public enum NavigationType
    {
        TopMenu,
        SideMenu,
        Both,
        Footer,
        Hidden
    }

    public enum PermissionType
    {
        Read,
        Write,
        Delete,
        Execute,
        Admin
    }
}