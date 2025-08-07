using System;

namespace BlazorShell.Application.Models;

public class ModuleChangedEventArgs : EventArgs
{
    public string ModuleName { get; set; } = string.Empty;
    public FileChangeType ChangeType { get; set; }
    public DateTime Timestamp { get; set; }
}
