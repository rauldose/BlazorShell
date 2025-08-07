using System;

namespace BlazorShell.Core.Services;

public class ModuleChangedEventArgs : EventArgs
{
    public string ModuleName { get; set; } = string.Empty;
    public FileChangeType ChangeType { get; set; }
    public DateTime Timestamp { get; set; }
}
