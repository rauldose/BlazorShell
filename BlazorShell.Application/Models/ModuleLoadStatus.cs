using System;

namespace BlazorShell.Application.Models;

public class ModuleLoadStatus
{
    public string ModuleName { get; set; } = string.Empty;
    public ModuleState State { get; set; }
    public bool IsCore { get; set; }
    public int Priority { get; set; }
    public string? LastError { get; set; }
    public DateTime LastStateChange { get; set; }
    public DateTime? LastAccessTime { get; set; }
}
