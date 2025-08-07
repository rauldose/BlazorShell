namespace BlazorShell.Modules.Admin.Services.Models;

public class ModuleOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
}
