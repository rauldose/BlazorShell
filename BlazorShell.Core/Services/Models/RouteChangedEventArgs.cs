using System;

namespace BlazorShell.Core.Services;

public class RouteChangedEventArgs : EventArgs
{
    public string ModuleName { get; }
    public RouteChangeType ChangeType { get; }
    public DateTime Timestamp { get; }

    public RouteChangedEventArgs(string moduleName, RouteChangeType changeType)
    {
        ModuleName = moduleName;
        ChangeType = changeType;
        Timestamp = DateTime.UtcNow;
    }
}
