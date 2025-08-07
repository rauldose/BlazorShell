namespace BlazorShell.Modules.Admin.Services.Models;

public class ModuleUploadResult : ModuleOperationResult
{
    public string? ModuleName { get; set; }
    public string? Version { get; set; }
}
