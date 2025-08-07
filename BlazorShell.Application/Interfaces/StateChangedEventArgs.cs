namespace BlazorShell.Application.Interfaces;

public class StateChangedEventArgs : EventArgs
{
    public string Key { get; }
    public object OldValue { get; }
    public object NewValue { get; }

    public StateChangedEventArgs(string key, object oldValue, object newValue)
    {
        Key = key;
        OldValue = oldValue;
        NewValue = newValue;
    }
}

