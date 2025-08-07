using BlazorShell.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Modules.Admin.Services;

public interface IModuleManagementUIService
{
    Task<IEnumerable<ModuleInfo>> LoadModulesAsync();
    Task<bool> EnableModuleAsync(string moduleName);
    Task<bool> DisableModuleAsync(string moduleName);
    Task<bool> ReloadModuleAsync(string moduleName);
    Task<bool> UninstallModuleAsync(string moduleName);
    Task<IEnumerable<ModuleInfo>> FilterModulesAsync(
        IEnumerable<ModuleInfo> modules, 
        string searchTerm, 
        string filterCategory, 
        string filterStatus);
    Task<List<string>> GetModuleCategoriesAsync(IEnumerable<ModuleInfo> modules);
}

public class ModuleManagementUIService : IModuleManagementUIService
{
    private readonly IModuleManagementService _moduleManagementService;
    private readonly ILogger<ModuleManagementUIService> _logger;

    public ModuleManagementUIService(
        IModuleManagementService moduleManagementService,
        ILogger<ModuleManagementUIService> logger)
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
            _logger.LogInformation("Loaded {Count} modules", modules.Count());
            return modules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading modules");
            throw;
        }
    }

    public async Task<bool> EnableModuleAsync(string moduleName)
    {
        try
        {
            _logger.LogInformation("Enabling module: {ModuleName}", moduleName);
            await _moduleManagementService.EnableModuleAsync(moduleName);
            _logger.LogInformation("Module {ModuleName} enabled successfully", moduleName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling module: {ModuleName}", moduleName);
            return false;
        }
    }

    public async Task<bool> DisableModuleAsync(string moduleName)
    {
        try
        {
            _logger.LogInformation("Disabling module: {ModuleName}", moduleName);
            await _moduleManagementService.DisableModuleAsync(moduleName);
            _logger.LogInformation("Module {ModuleName} disabled successfully", moduleName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling module: {ModuleName}", moduleName);
            return false;
        }
    }

    public async Task<bool> ReloadModuleAsync(string moduleName)
    {
        try
        {
            _logger.LogInformation("Reloading module: {ModuleName}", moduleName);
            await _moduleManagementService.ReloadModuleAsync(moduleName);
            _logger.LogInformation("Module {ModuleName} reloaded successfully", moduleName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading module: {ModuleName}", moduleName);
            return false;
        }
    }

    public async Task<bool> UninstallModuleAsync(string moduleName)
    {
        try
        {
            _logger.LogInformation("Uninstalling module: {ModuleName}", moduleName);
            await _moduleManagementService.UninstallModuleAsync(moduleName);
            _logger.LogInformation("Module {ModuleName} uninstalled successfully", moduleName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uninstalling module: {ModuleName}", moduleName);
            return false;
        }
    }

    public Task<IEnumerable<ModuleInfo>> FilterModulesAsync(
        IEnumerable<ModuleInfo> modules, 
        string searchTerm, 
        string filterCategory, 
        string filterStatus)
    {
        var filtered = modules.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            filtered = filtered.Where(m =>
                m.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                m.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                m.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filterCategory))
        {
            filtered = filtered.Where(m => m.Category.Equals(filterCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filterStatus))
        {
            var isEnabled = filterStatus.Equals("Enabled", StringComparison.OrdinalIgnoreCase);
            filtered = filtered.Where(m => m.IsEnabled == isEnabled);
        }

        return Task.FromResult(filtered);
    }

    public Task<List<string>> GetModuleCategoriesAsync(IEnumerable<ModuleInfo> modules)
    {
        var categories = modules
            .Where(m => !string.IsNullOrEmpty(m.Category))
            .Select(m => m.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        return Task.FromResult(categories);
    }
}