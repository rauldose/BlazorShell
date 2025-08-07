// Infrastructure/Services/ModuleManagementService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using BlazorShell.Application.Interfaces;
using BlazorShell.Domain.Entities;
using BlazorShell.Infrastructure.Data;
using Newtonsoft.Json;
using Module = BlazorShell.Domain.Entities.Module;

namespace BlazorShell.Modules.Admin.Services
{
    public class ModuleManagementService : IModuleManagementService
    {
        private readonly IModuleLoader _moduleLoader;
        private readonly IModuleRegistry _moduleRegistry;
        private readonly IPluginAssemblyLoader _assemblyLoader;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ModuleManagementService> _logger;
        private readonly string _modulesPath;
        private readonly string _moduleBackupPath;

        public ModuleManagementService(
            IModuleLoader moduleLoader,
            IModuleRegistry moduleRegistry,
            IPluginAssemblyLoader assemblyLoader,
            ApplicationDbContext dbContext,
            ILogger<ModuleManagementService> logger)
        {
            _moduleLoader = moduleLoader;
            _moduleRegistry = moduleRegistry;
            _assemblyLoader = assemblyLoader;
            _dbContext = dbContext;
            _logger = logger;
            _modulesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules");
            _moduleBackupPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModuleBackups");

            EnsureDirectoriesExist();
        }

        private void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(_modulesPath))
                Directory.CreateDirectory(_modulesPath);
            if (!Directory.Exists(_moduleBackupPath))
                Directory.CreateDirectory(_moduleBackupPath);
        }

        public async Task<IEnumerable<ModuleInfo>> GetAllModulesAsync()
        {
            var moduleInfos = new List<ModuleInfo>();

            // Get loaded modules from registry
            var loadedModules = _moduleRegistry.GetModules();

            // Get all modules from database
            var dbModules = await _dbContext.Modules.ToListAsync();

            // Combine information
            foreach (var dbModule in dbModules)
            {
                var loadedModule = loadedModules.FirstOrDefault(m => m.Name == dbModule.Name);

                var info = new ModuleInfo
                {
                    Name = dbModule.Name ?? string.Empty,
                    DisplayName = dbModule.DisplayName ?? string.Empty,
                    Description = dbModule.Description ?? string.Empty,
                    Version = dbModule.Version ?? "1.0.0",
                    Author = dbModule.Author ?? "Unknown",
                    Icon = dbModule.Icon ?? "bi bi-puzzle",
                    Category = dbModule.Category ?? "General",
                    IsEnabled = dbModule.IsEnabled,
                    IsLoaded = loadedModule != null,
                    IsCore = dbModule.IsCore,
                    LoadOrder = dbModule.LoadOrder,
                    AssemblyPath = Path.Combine(_modulesPath, dbModule.AssemblyName ?? $"{dbModule.Name}.dll"),
                    LastModified = dbModule.ModifiedDate ?? dbModule.CreatedDate,
                    FileSize = GetModuleFileSize(dbModule.AssemblyName),
                    Status = DetermineModuleStatus(dbModule, loadedModule)
                };

                // Add runtime information if loaded
                if (loadedModule != null)
                {
                    info.LoadedAt = DateTime.UtcNow; // You might want to track this properly
                    info.ComponentCount = loadedModule.GetComponentTypes()?.Count() ?? 0;
                    info.NavigationItemCount = loadedModule.GetNavigationItems()?.Count() ?? 0;
                }

                moduleInfos.Add(info);
            }

            // Check for orphaned DLL files (not in database)
            var dllFiles = Directory.GetFiles(_modulesPath, "*.dll");
            foreach (var dllFile in dllFiles)
            {
                var fileName = Path.GetFileName(dllFile);
                if (!dbModules.Any(m => m.AssemblyName == fileName))
                {
                    moduleInfos.Add(new ModuleInfo
                    {
                        Name = Path.GetFileNameWithoutExtension(fileName),
                        DisplayName = Path.GetFileNameWithoutExtension(fileName),
                        Description = "Unregistered module found in modules folder",
                        Version = "Unknown",
                        Author = "Unknown",
                        Icon = "bi bi-question-circle",
                        Category = "Unregistered",
                        IsEnabled = false,
                        IsLoaded = false,
                        IsCore = false,
                        AssemblyPath = dllFile,
                        FileSize = new FileInfo(dllFile).Length,
                        Status = ModuleStatus.Unregistered
                    });
                }
            }

            return moduleInfos.OrderBy(m => m.LoadOrder).ThenBy(m => m.Name);
        }

        public async Task<ModuleInfo?> GetModuleAsync(string moduleName)
        {
            var modules = await GetAllModulesAsync();
            return modules.FirstOrDefault(m => m.Name == moduleName);
        }

        public async Task<ModuleOperationResult> EnableModuleAsync(string moduleName)
        {
            try
            {
                var dbModule = await _dbContext.Modules
                    .FirstOrDefaultAsync(m => m.Name == moduleName);

                if (dbModule == null)
                {
                    return new ModuleOperationResult
                    {
                        Success = false,
                        Message = $"Module {moduleName} not found in database"
                    };
                }

                // Check if already loaded
                if (_moduleRegistry.IsModuleRegistered(moduleName))
                {
                    return new ModuleOperationResult
                    {
                        Success = false,
                        Message = $"Module {moduleName} is already loaded"
                    };
                }

                // Load the module
                var assemblyPath = Path.Combine(_modulesPath, dbModule.AssemblyName ?? $"{moduleName}.dll");
                if (!File.Exists(assemblyPath))
                {
                    return new ModuleOperationResult
                    {
                        Success = false,
                        Message = $"Module assembly not found: {assemblyPath}"
                    };
                }

                var module = await _moduleLoader.LoadModuleAsync(assemblyPath);
                if (module != null)
                {
                    dbModule.IsEnabled = true;
                    await _dbContext.SaveChangesAsync();

                    return new ModuleOperationResult
                    {
                        Success = true,
                        Message = $"Module {moduleName} enabled successfully"
                    };
                }

                return new ModuleOperationResult
                {
                    Success = false,
                    Message = $"Failed to load module {moduleName}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling module {ModuleName}", moduleName);
                return new ModuleOperationResult
                {
                    Success = false,
                    Message = $"Error enabling module: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<ModuleOperationResult> DisableModuleAsync(string moduleName)
        {
            try
            {
                var dbModule = await _dbContext.Modules
                    .FirstOrDefaultAsync(m => m.Name == moduleName);

                if (dbModule == null)
                {
                    return new ModuleOperationResult
                    {
                        Success = false,
                        Message = $"Module {moduleName} not found"
                    };
                }

                if (dbModule.IsCore)
                {
                    return new ModuleOperationResult
                    {
                        Success = false,
                        Message = $"Cannot disable core module {moduleName}"
                    };
                }

                var success = await _moduleLoader.UnloadModuleAsync(moduleName);
                if (success)
                {
                    dbModule.IsEnabled = false;
                    await _dbContext.SaveChangesAsync();

                    return new ModuleOperationResult
                    {
                        Success = true,
                        Message = $"Module {moduleName} disabled successfully"
                    };
                }

                return new ModuleOperationResult
                {
                    Success = false,
                    Message = $"Failed to unload module {moduleName}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling module {ModuleName}", moduleName);
                return new ModuleOperationResult
                {
                    Success = false,
                    Message = $"Error disabling module: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<ModuleOperationResult> ReloadModuleAsync(string moduleName)
        {
            try
            {
                await _moduleLoader.ReloadModuleAsync(moduleName);
                return new ModuleOperationResult
                {
                    Success = true,
                    Message = $"Module {moduleName} reloaded successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading module {ModuleName}", moduleName);
                return new ModuleOperationResult
                {
                    Success = false,
                    Message = $"Error reloading module: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<ModuleOperationResult> UninstallModuleAsync(string moduleName)
        {
            try
            {
                // First disable the module
                var disableResult = await DisableModuleAsync(moduleName);
                if (!disableResult.Success)
                {
                    return disableResult;
                }

                // Remove from database
                var dbModule = await _dbContext.Modules
                    .FirstOrDefaultAsync(m => m.Name == moduleName);

                if (dbModule != null)
                {
                    // Backup the module file before deletion
                    var assemblyPath = Path.Combine(_modulesPath, dbModule.AssemblyName ?? $"{moduleName}.dll");
                    if (File.Exists(assemblyPath))
                    {
                        var backupPath = Path.Combine(_moduleBackupPath,
                            $"{moduleName}_{DateTime.Now:yyyyMMddHHmmss}.dll");
                        File.Move(assemblyPath, backupPath);
                    }

                    _dbContext.Modules.Remove(dbModule);
                    await _dbContext.SaveChangesAsync();
                }

                return new ModuleOperationResult
                {
                    Success = true,
                    Message = $"Module {moduleName} uninstalled successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uninstalling module {ModuleName}", moduleName);
                return new ModuleOperationResult
                {
                    Success = false,
                    Message = $"Error uninstalling module: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<ModuleUploadResult> UploadModuleAsync(Stream fileStream, string fileName)
        {
            try
            {
                // Validate file extension
                if (!fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    return new ModuleUploadResult
                    {
                        Success = false,
                        Message = "Invalid file type. Only DLL files are allowed."
                    };
                }

                // Save file temporarily
                var tempPath = Path.Combine(Path.GetTempPath(), fileName);
                using (var fs = new FileStream(tempPath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(fs);
                }

                // Validate the module
                if (!await ValidateModuleAsync(tempPath))
                {
                    File.Delete(tempPath);
                    return new ModuleUploadResult
                    {
                        Success = false,
                        Message = "Module validation failed. The DLL does not contain a valid IModule implementation."
                    };
                }

                // Load assembly to get module info
                var assembly = _assemblyLoader.LoadPlugin(tempPath);
                var moduleTypes = _assemblyLoader.GetTypesFromAssembly(assembly, typeof(IModule));
                var moduleType = moduleTypes.FirstOrDefault();

                if (moduleType == null)
                {
                    File.Delete(tempPath);
                    return new ModuleUploadResult
                    {
                        Success = false,
                        Message = "No IModule implementation found in the assembly."
                    };
                }

                var moduleInstance = _assemblyLoader.CreateInstance<IModule>(moduleType);
                if (moduleInstance == null)
                {
                    File.Delete(tempPath);
                    return new ModuleUploadResult
                    {
                        Success = false,
                        Message = "Failed to create module instance."
                    };
                }

                // Check if module already exists
                var existingModule = await _dbContext.Modules
                    .FirstOrDefaultAsync(m => m.Name == moduleInstance.Name);

                if (existingModule != null)
                {
                    File.Delete(tempPath);
                    return new ModuleUploadResult
                    {
                        Success = false,
                        Message = $"Module {moduleInstance.Name} already exists.",
                        ModuleName = moduleInstance.Name
                    };
                }

                // Move file to modules folder
                var targetPath = Path.Combine(_modulesPath, fileName);
                if (File.Exists(targetPath))
                {
                    // Backup existing file
                    var backupPath = Path.Combine(_moduleBackupPath,
                        $"{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.Now:yyyyMMddHHmmss}.dll");
                    File.Move(targetPath, backupPath);
                }

                File.Move(tempPath, targetPath);

                // Add to database
                var dbModule = new Module
                {
                    Name = moduleInstance.Name,
                    DisplayName = moduleInstance.DisplayName,
                    Description = moduleInstance.Description,
                    Version = moduleInstance.Version,
                    Author = moduleInstance.Author,
                    Icon = moduleInstance.Icon,
                    Category = moduleInstance.Category,
                    LoadOrder = moduleInstance.Order,
                    AssemblyName = fileName,
                    EntryType = moduleType.FullName,
                    IsEnabled = false,
                    IsCore = false,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                };

                _dbContext.Modules.Add(dbModule);
                await _dbContext.SaveChangesAsync();

                return new ModuleUploadResult
                {
                    Success = true,
                    Message = $"Module {moduleInstance.Name} uploaded successfully.",
                    ModuleName = moduleInstance.Name,
                    Version = moduleInstance.Version
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading module {FileName}", fileName);
                return new ModuleUploadResult
                {
                    Success = false,
                    Message = $"Error uploading module: {ex.Message}"
                };
            }
        }

        public async Task<bool> ValidateModuleAsync(string assemblyPath)
        {
            try
            {
                var assembly = Assembly.LoadFrom(assemblyPath);
                var hasIModule = assembly.GetTypes()
                    .Any(t => typeof(IModule).IsAssignableFrom(t) && !t.IsAbstract);

                return await Task.FromResult(hasIModule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating module at {Path}", assemblyPath);
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetModuleConfigurationAsync(string moduleName)
        {
            var dbModule = await _dbContext.Modules
                .FirstOrDefaultAsync(m => m.Name == moduleName);

            if (dbModule?.Configuration != null)
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(dbModule.Configuration)
                    ?? new Dictionary<string, object>();
            }

            // Try to get default configuration from loaded module
            var module = _moduleRegistry.GetModule(moduleName);
            if (module != null)
            {
                return module.GetDefaultSettings();
            }

            return new Dictionary<string, object>();
        }

        public async Task<bool> UpdateModuleConfigurationAsync(string moduleName, Dictionary<string, object> configuration)
        {
            try
            {
                var dbModule = await _dbContext.Modules
                    .FirstOrDefaultAsync(m => m.Name == moduleName);

                if (dbModule == null)
                    return false;

                dbModule.Configuration = JsonConvert.SerializeObject(configuration);
                dbModule.ModifiedDate = DateTime.UtcNow;
                dbModule.ModifiedBy = "System";

                await _dbContext.SaveChangesAsync();

                // If module is loaded and configurable, apply configuration
                var module = _moduleRegistry.GetModule(moduleName);
                if (module is IConfigurableModule configurableModule)
                {
                    await configurableModule.ApplyConfigurationAsync(configuration);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating configuration for module {ModuleName}", moduleName);
                return false;
            }
        }

        public async Task<IEnumerable<ModuleDependency>> GetModuleDependenciesAsync(string moduleName)
        {
            var dependencies = new List<ModuleDependency>();

            var dbModule = await _dbContext.Modules
                .FirstOrDefaultAsync(m => m.Name == moduleName);

            if (dbModule?.Dependencies != null)
            {
                var deps = JsonConvert.DeserializeObject<List<string>>(dbModule.Dependencies) ?? new List<string>();
                foreach (var dep in deps)
                {
                    var depModule = await _dbContext.Modules
                        .FirstOrDefaultAsync(m => m.Name == dep);

                    dependencies.Add(new ModuleDependency
                    {
                        ModuleName = dep,
                        IsRequired = true,
                        IsLoaded = _moduleRegistry.IsModuleRegistered(dep),
                        IsSatisfied = depModule != null && depModule.IsEnabled
                    });
                }
            }

            return dependencies;
        }

        public async Task<ModuleHealthStatus> GetModuleHealthAsync(string moduleName)
        {
            var health = new ModuleHealthStatus
            {
                ModuleName = moduleName,
                CheckTime = DateTime.UtcNow
            };

            try
            {
                var module = _moduleRegistry.GetModule(moduleName);
                if (module == null)
                {
                    health.IsHealthy = false;
                    health.Status = "Not Loaded";
                    return health;
                }

                // Check dependencies
                var dependencies = await GetModuleDependenciesAsync(moduleName);
                var unsatisfiedDeps = dependencies.Where(d => !d.IsSatisfied).ToList();

                if (unsatisfiedDeps.Any())
                {
                    health.IsHealthy = false;
                    health.Status = "Missing Dependencies";
                    health.Issues.Add($"Missing dependencies: {string.Join(", ", unsatisfiedDeps.Select(d => d.ModuleName))}");
                }
                else
                {
                    health.IsHealthy = true;
                    health.Status = "Healthy";
                }

                // Add metrics
                health.Metrics["ComponentCount"] = module.GetComponentTypes()?.Count() ?? 0;
                health.Metrics["NavigationItemCount"] = module.GetNavigationItems()?.Count() ?? 0;

                return health;
            }
            catch (Exception ex)
            {
                health.IsHealthy = false;
                health.Status = "Error";
                health.Issues.Add($"Health check error: {ex.Message}");
                return health;
            }
        }

        private long GetModuleFileSize(string? assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName))
                return 0;

            var path = Path.Combine(_modulesPath, assemblyName);
            if (File.Exists(path))
            {
                return new FileInfo(path).Length;
            }

            return 0;
        }

        private ModuleStatus DetermineModuleStatus(Module dbModule, IModule? loadedModule)
        {
            if (loadedModule != null)
                return ModuleStatus.Running;
            if (dbModule.IsEnabled)
                return ModuleStatus.Stopped;
            if (!File.Exists(Path.Combine(_modulesPath, dbModule.AssemblyName ?? $"{dbModule.Name}.dll")))
                return ModuleStatus.Missing;
            return ModuleStatus.Disabled;
        }
    }

    // Supporting classes
    public class ModuleInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public bool IsLoaded { get; set; }
        public bool IsCore { get; set; }
        public int LoadOrder { get; set; }
        public string AssemblyPath { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public DateTime? LoadedAt { get; set; }
        public long FileSize { get; set; }
        public int ComponentCount { get; set; }
        public int NavigationItemCount { get; set; }
        public ModuleStatus Status { get; set; }
    }

    public enum ModuleStatus
    {
        Running,
        Stopped,
        Disabled,
        Missing,
        Error,
        Unregistered
    }

    public class ModuleOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
    }

    public class ModuleUploadResult : ModuleOperationResult
    {
        public string? ModuleName { get; set; }
        public string? Version { get; set; }
    }

    public class ModuleDependency
    {
        public string ModuleName { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public bool IsLoaded { get; set; }
        public bool IsSatisfied { get; set; }
    }

    public class ModuleHealthStatus
    {
        public string ModuleName { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<string> Issues { get; set; } = new();
        public Dictionary<string, object> Metrics { get; set; } = new();
        public DateTime CheckTime { get; set; }
    }
}
