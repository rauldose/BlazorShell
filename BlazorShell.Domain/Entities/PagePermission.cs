namespace BlazorShell.Domain.Entities;

public class PagePermission
{
    public int Id { get; set; }
    public int NavigationItemId { get; set; }
    public string? UserId { get; set; }
    public string? RoleId { get; set; }
    public string? PermissionType { get; set; }
    public bool IsGranted { get; set; }
    public DateTime GrantedDate { get; set; }
    public string? GrantedBy { get; set; }

    // Navigation properties
    public virtual NavigationItem NavigationItem { get; set; }
    public virtual ApplicationUser User { get; set; }
    public virtual ApplicationRole Role { get; set; }
}
