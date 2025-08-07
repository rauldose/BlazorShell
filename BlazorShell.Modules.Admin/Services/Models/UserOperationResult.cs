namespace BlazorShell.Modules.Admin.Services.Models;

public class UserOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? UserId { get; set; }
}
