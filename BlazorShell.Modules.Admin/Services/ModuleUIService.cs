using BlazorShell.Modules.Admin.Services;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Modules.Admin.Services
{
    public interface IModuleUIService
    {
        Task<IEnumerable<ModuleInfo>> LoadModulesAsync();
        Task<ModuleOperationResult> EnableModuleAsync(string moduleName);
        Task<ModuleOperationResult> DisableModuleAsync(string moduleName);
        Task<ModuleOperationResult> ReloadModuleAsync(string moduleName);
        Task<ModuleOperationResult> UninstallModuleAsync(string moduleName);
        Task<IEnumerable<ModuleInfo>> FilterModulesAsync(IEnumerable<ModuleInfo> modules, string? searchTerm, string? category, string? status);
        IEnumerable<string> GetCategoriesFromModules(IEnumerable<ModuleInfo> modules);
    }

    public class ModuleUIService : IModuleUIService
    {
        private readonly IModuleManagementService _moduleManagementService;
        private readonly ILogger<ModuleUIService> _logger;

        public ModuleUIService(
            IModuleManagementService moduleManagementService,
            ILogger<ModuleUIService> logger)
        {
            _moduleManagementService = moduleManagementService;
            _logger = logger;
        }

        public async Task<IEnumerable<ModuleInfo>> LoadModulesAsync()
        {
            try
            {
                _logger.LogInformation("Loading modules");
                var modules = await _moduleManagementService.GetAllModulesAsync();
                _logger.LogInformation("Successfully loaded {Count} modules", modules.Count());
                return modules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading modules");
                throw;
            }
        }

        public async Task<ModuleOperationResult> EnableModuleAsync(string moduleName)
        {
            try
            {
                _logger.LogInformation("Enabling module: {ModuleName}", moduleName);
                var result = await _moduleManagementService.EnableModuleAsync(moduleName);
                
                if (result.Success)
                {
                    _logger.LogInformation("Successfully enabled module: {ModuleName}", moduleName);
                }
                else
                {
                    _logger.LogWarning("Failed to enable module {ModuleName}: {Message}", moduleName, result.Message);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling module: {ModuleName}", moduleName);
                return new ModuleOperationResult { Success = false, Message = $"Error enabling module: {ex.Message}" };
            }
        }

        public async Task<ModuleOperationResult> DisableModuleAsync(string moduleName)
        {
            try
            {
                _logger.LogInformation("Disabling module: {ModuleName}", moduleName);
                var result = await _moduleManagementService.DisableModuleAsync(moduleName);
                
                if (result.Success)
                {
                    _logger.LogInformation("Successfully disabled module: {ModuleName}", moduleName);
                }
                else
                {
                    _logger.LogWarning("Failed to disable module {ModuleName}: {Message}", moduleName, result.Message);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling module: {ModuleName}", moduleName);
                return new ModuleOperationResult { Success = false, Message = $"Error disabling module: {ex.Message}" };
            }
        }

        public async Task<ModuleOperationResult> ReloadModuleAsync(string moduleName)
        {
            try
            {
                _logger.LogInformation("Reloading module: {ModuleName}", moduleName);
                var result = await _moduleManagementService.ReloadModuleAsync(moduleName);
                
                if (result.Success)
                {
                    _logger.LogInformation("Successfully reloaded module: {ModuleName}", moduleName);
                }
                else
                {
                    _logger.LogWarning("Failed to reload module {ModuleName}: {Message}", moduleName, result.Message);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading module: {ModuleName}", moduleName);
                return new ModuleOperationResult { Success = false, Message = $"Error reloading module: {ex.Message}" };
            }
        }

        public async Task<ModuleOperationResult> UninstallModuleAsync(string moduleName)
        {
            try
            {
                _logger.LogInformation("Uninstalling module: {ModuleName}", moduleName);
                var result = await _moduleManagementService.UninstallModuleAsync(moduleName);
                
                if (result.Success)
                {
                    _logger.LogInformation("Successfully uninstalled module: {ModuleName}", moduleName);
                }
                else
                {
                    _logger.LogWarning("Failed to uninstall module {ModuleName}: {Message}", moduleName, result.Message);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uninstalling module: {ModuleName}", moduleName);
                return new ModuleOperationResult { Success = false, Message = $"Error uninstalling module: {ex.Message}" };
            }
        }

        public Task<IEnumerable<ModuleInfo>> FilterModulesAsync(IEnumerable<ModuleInfo> modules, string? searchTerm, string? category, string? status)
        {
            var filtered = modules.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                filtered = filtered.Where(m =>
                    m.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    m.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    m.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                filtered = filtered.Where(m => m.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                filtered = filtered.Where(m => m.Status.ToString() == status);
            }

            return Task.FromResult(filtered);
        }

        public IEnumerable<string> GetCategoriesFromModules(IEnumerable<ModuleInfo> modules)
        {
            return modules.Select(m => m.Category).Distinct().OrderBy(c => c);
        }
    }
}