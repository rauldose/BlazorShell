using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace BlazorShell.Domain.Entities;

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

