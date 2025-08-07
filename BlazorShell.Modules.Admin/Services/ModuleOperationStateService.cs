namespace BlazorShell.Modules.Admin.Services
{
    public interface IModuleOperationStateService
    {
        bool IsLoading { get; }
        bool IsOperating(string moduleName);
        IEnumerable<string> OperatingModules { get; }
        
        event EventHandler? StateChanged;
        
        void SetLoading(bool isLoading);
        Task ExecuteOperationAsync(string moduleName, Func<Task> operation);
        Task<T> ExecuteOperationAsync<T>(string moduleName, Func<Task<T>> operation);
    }

    public class ModuleOperationStateService : IModuleOperationStateService
    {
        private readonly HashSet<string> _operatingModules = new();
        private bool _isLoading;

        public bool IsLoading => _isLoading;
        public IEnumerable<string> OperatingModules => _operatingModules.ToList();
        
        public event EventHandler? StateChanged;

        public bool IsOperating(string moduleName)
        {
            return _operatingModules.Contains(moduleName);
        }

        public void SetLoading(bool isLoading)
        {
            if (_isLoading != isLoading)
            {
                _isLoading = isLoading;
                OnStateChanged();
            }
        }

        public async Task ExecuteOperationAsync(string moduleName, Func<Task> operation)
        {
            _operatingModules.Add(moduleName);
            OnStateChanged();
            
            try
            {
                await operation();
            }
            finally
            {
                _operatingModules.Remove(moduleName);
                OnStateChanged();
            }
        }

        public async Task<T> ExecuteOperationAsync<T>(string moduleName, Func<Task<T>> operation)
        {
            _operatingModules.Add(moduleName);
            OnStateChanged();
            
            try
            {
                return await operation();
            }
            finally
            {
                _operatingModules.Remove(moduleName);
                OnStateChanged();
            }
        }

        private void OnStateChanged()
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}