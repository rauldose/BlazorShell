// BlazorShell.Modules.Admin/Services/Interfaces/IModuleManagementService.cs
using BlazorShell.Domain.Entities;
using System.Collections.Generic;
using System.IO;

namespace BlazorShell.Modules.Admin.Services;

public interface IModuleManagementService
{
    Task<IEnumerable<ModuleInfo>> GetAllModulesAsync();
    Task<ModuleInfo?> GetModuleAsync(string moduleName);
    Task<ModuleOperationResult> EnableModuleAsync(string moduleName);
    Task<ModuleOperationResult> DisableModuleAsync(string moduleName);
    Task<ModuleOperationResult> ReloadModuleAsync(string moduleName);
    Task<ModuleOperationResult> UninstallModuleAsync(string moduleName);
    Task<ModuleUploadResult> UploadModuleAsync(Stream fileStream, string fileName);
    Task<bool> ValidateModuleAsync(string assemblyPath);
    Task<Dictionary<string, object>> GetModuleConfigurationAsync(string moduleName);
    Task<bool> UpdateModuleConfigurationAsync(string moduleName, Dictionary<string, object> configuration);
    Task<IEnumerable<ModuleDependency>> GetModuleDependenciesAsync(string moduleName);
    Task<ModuleHealthStatus> GetModuleHealthAsync(string moduleName);
}

