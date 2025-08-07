namespace BlazorShell.Application.Interfaces;

public class ModuleEventArgs : EventArgs
{
    public IModule Module { get; }
    public string Action { get; }
    public DateTime Timestamp { get; }

    public ModuleEventArgs(IModule module, string action)
    {
        Module = module;
        Action = action;
        Timestamp = DateTime.UtcNow;
    }
}

