namespace BlazorShell.Core.Entities
{
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
        public virtual Module Module { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ApplicationRole Role { get; set; } = null!;
    }
}