namespace BlazorShell.Core.Interfaces
{
    /// <summary>
    /// Event args for module events
    /// </summary>
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
}