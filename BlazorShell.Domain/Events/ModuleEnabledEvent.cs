namespace BlazorShell.Domain.Events;

public class ModuleEnabledEvent : IDomainEvent
{
    public string ModuleName { get; }

    public ModuleEnabledEvent(string moduleName)
    {
        ModuleName = moduleName;
    }
}
