namespace BlazorShell.Modules.Admin.Services.Models;

public class CreateUserModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string>? Roles { get; set; }
}
