namespace BlazorShell.Core.Interfaces
{
    /// <summary>
    /// State container for managing application state
    /// </summary>
    public interface IStateContainer
    {
        T GetState<T>(string key) where T : class;
        void SetState<T>(string key, T value) where T : class;
        bool RemoveState(string key);
        void ClearState();
        event EventHandler<StateChangedEventArgs> StateChanged;
    }
}