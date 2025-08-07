namespace BlazorShell.Modules.Admin.Services.Models;

public class ModuleHealthStatus
{
    public string ModuleName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Issues { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
    public DateTime CheckTime { get; set; }
}
