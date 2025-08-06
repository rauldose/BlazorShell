# Move-And-Split-BlazorShell.ps1
# This script moves your existing files to the new project structure and splits large files

param(
    [string]$SolutionPath = "D:\temp\_myProj\BlazorShell",
    [switch]$DryRun = $false
)

$ErrorActionPreference = "Continue"

function Write-Status {
    param([string]$Message, [string]$Type = "Info")
    $color = switch($Type) {
        "Success" { "Green" }
        "Warning" { "Yellow" }
        "Error" { "Red" }
        default { "Cyan" }
    }
    Write-Host "[$Type] $Message" -ForegroundColor $color
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Moving & Splitting BlazorShell Files " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($DryRun) {
    Write-Status "DRY RUN MODE - No files will be moved" "Warning"
}

# Function to ensure directory exists
function Ensure-Directory {
    param([string]$Path)
    if (-not (Test-Path $Path)) {
        if (-not $DryRun) {
            New-Item -ItemType Directory -Path $Path -Force | Out-Null
        }
        Write-Status "  Created directory: $Path" "Success"
    }
}

# Function to move or copy a file
function Move-Or-Copy-File {
    param(
        [string]$Source,
        [string]$Destination,
        [switch]$Copy = $false
    )
    
    if (Test-Path $Source) {
        $destDir = Split-Path $Destination -Parent
        Ensure-Directory -Path $destDir
        
        if (-not $DryRun) {
            if ($Copy) {
                Copy-Item -Path $Source -Destination $Destination -Force
                Write-Host "    Copied: $(Split-Path $Source -Leaf) -> $(Split-Path $Destination -Parent | Split-Path -Leaf)" -ForegroundColor Green
            } else {
                Move-Item -Path $Source -Destination $Destination -Force -ErrorAction SilentlyContinue
                if (-not (Test-Path $Source)) {
                    Write-Host "    Moved: $(Split-Path $Source -Leaf) -> $(Split-Path $Destination -Parent | Split-Path -Leaf)" -ForegroundColor Green
                } else {
                    Copy-Item -Path $Source -Destination $Destination -Force
                    Write-Host "    Copied: $(Split-Path $Source -Leaf) -> $(Split-Path $Destination -Parent | Split-Path -Leaf)" -ForegroundColor Yellow
                }
            }
        } else {
            Write-Host "    Would move: $(Split-Path $Source -Leaf) -> $Destination" -ForegroundColor Gray
        }
        return $true
    }
    return $false
}

# =======================
# STEP 1: SPLIT ENTITIES.CS
# =======================
Write-Status "Step 1: Splitting and moving Entities.cs" "Info"

$entitiesPath = Join-Path $SolutionPath "BlazorShell\Core\Entities\Entities.cs"
$domainEntitiesPath = Join-Path $SolutionPath "1_Core\BlazorShell.Domain\Entities"

if (Test-Path $entitiesPath) {
    $content = Get-Content $entitiesPath -Raw
    
    # Extract namespace
    if ($content -match "namespace\s+([\w\.]+)") {
        $originalNamespace = $matches[1]
    }
    
    # Extract usings
    $usings = @()
    $content -split "`n" | ForEach-Object {
        if ($_ -match "^using\s+") {
            $usings += $_
        }
    }
    
    # Define entities to extract
    $entities = @{
        "ApplicationUser.cs" = @("public class ApplicationUser", "}")
        "ApplicationRole.cs" = @("public class ApplicationRole", "}")
        "Module.cs" = @("public class Module", "}")
        "ModulePermission.cs" = @("public class ModulePermission", "}")
        "NavigationItem.cs" = @("public class NavigationItem", "}")
        "AuditLog.cs" = @("public class AuditLog", "}")
        "Setting.cs" = @("public class Setting", "}")
        "IAuditableEntity.cs" = @("public interface IAuditableEntity", "}")
    }
    
    foreach ($fileName in $entities.Keys) {
        $outputPath = Join-Path $domainEntitiesPath $fileName
        
        if (-not $DryRun) {
            Ensure-Directory -Path $domainEntitiesPath
            
            # Create file content
            $fileContent = @()
            $fileContent += "using Microsoft.AspNetCore.Identity;"
            $fileContent += "using System;"
            $fileContent += "using System.Collections.Generic;"
            $fileContent += ""
            $fileContent += "namespace BlazorShell.Domain.Entities"
            $fileContent += "{"
            
            # Extract the class/interface content (simplified - you may need to adjust)
            $pattern = $entities[$fileName][0]
            if ($content -match "($pattern[\s\S]*?\n\s*}\s*(?=\n\s*(public|internal|private|protected|//|$)))") {
                $classContent = $matches[1]
                $fileContent += "    " + $classContent
            }
            
            $fileContent += "}"
            
            Set-Content -Path $outputPath -Value ($fileContent -join "`n")
            Write-Host "    Created: $fileName" -ForegroundColor Green
        }
    }
}

# =======================
# STEP 2: SPLIT INTERFACES.CS
# =======================
Write-Status "Step 2: Splitting and moving Interfaces.cs" "Info"

$interfacesPath = Join-Path $SolutionPath "BlazorShell\Core\Interfaces\Interfaces.cs"
$appInterfacesPath = Join-Path $SolutionPath "1_Core\BlazorShell.Application\Interfaces"

if (Test-Path $interfacesPath) {
    # Move the entire file first, then we'll split it
    Move-Or-Copy-File -Source $interfacesPath -Destination (Join-Path $appInterfacesPath "Interfaces.cs")
    
    # Define interfaces to extract
    $interfaces = @(
        "IModule",
        "IServiceModule", 
        "IConfigurableModule",
        "IModuleLoader",
        "IModuleRegistry",
        "IPluginAssemblyLoader",
        "IDynamicRouteService",
        "IModuleAuthorizationService",
        "INavigationService",
        "IStateContainer"
    )
    
    # Note: In production, you'd want to properly parse and split these
    Write-Status "  Interfaces moved. Manual splitting may be required." "Warning"
}

# =======================
# STEP 3: MOVE INFRASTRUCTURE FILES
# =======================
Write-Status "Step 3: Moving Infrastructure files" "Info"

# Data layer
$dataFiles = @(
    @{
        Source = "BlazorShell\Infrastructure\Data\ApplicationDbContext.cs"
        Dest = "2_Infrastructure\BlazorShell.Infrastructure\Data\ApplicationDbContext.cs"
    }
)

# Security
$securityFiles = @(
    @{
        Source = "BlazorShell\Infrastructure\Security\SecurityServices.cs"
        Dest = "2_Infrastructure\BlazorShell.Infrastructure\Security\SecurityServices.cs"
    }
)

# Module System files
$moduleSystemFiles = @(
    @{
        Source = "BlazorShell\Infrastructure\Services\ModuleLoader.cs"
        Dest = "2_Infrastructure\BlazorShell.ModuleSystem\Loader\ModuleLoader.cs"
    },
    @{
        Source = "BlazorShell\Infrastructure\Services\ModuleRegistry.cs"
        Dest = "2_Infrastructure\BlazorShell.ModuleSystem\Registry\ModuleRegistry.cs"
    },
    @{
        Source = "BlazorShell\Infrastructure\Services\PluginAssemblyLoader.cs"
        Dest = "2_Infrastructure\BlazorShell.ModuleSystem\Loader\PluginAssemblyLoader.cs"
    },
    @{
        Source = "BlazorShell\Infrastructure\Services\DynamicRouteService.cs"
        Dest = "2_Infrastructure\BlazorShell.ModuleSystem\Services\DynamicRouteService.cs"
    },
    @{
        Source = "BlazorShell\Infrastructure\Services\ModuleServiceProvider.cs"
        Dest = "2_Infrastructure\BlazorShell.ModuleSystem\Services\ModuleServiceProvider.cs"
    },
    @{
        Source = "BlazorShell\Infrastructure\Services\ModuleMetadataCache.cs"
        Dest = "2_Infrastructure\BlazorShell.ModuleSystem\Metadata\ModuleMetadataCache.cs"
    },
    @{
        Source = "BlazorShell\Infrastructure\Services\ModuleHotReloadService.cs"
        Dest = "2_Infrastructure\BlazorShell.ModuleSystem\Services\ModuleHotReloadService.cs"
    },
    @{
        Source = "BlazorShell\Infrastructure\Services\ModulePerformanceMonitor.cs"
        Dest = "2_Infrastructure\BlazorShell.ModuleSystem\Services\ModulePerformanceMonitor.cs"
    },
    @{
        Source = "BlazorShell\Infrastructure\Services\ModuleReloadCoordinator.cs"
        Dest = "2_Infrastructure\BlazorShell.ModuleSystem\Services\ModuleReloadCoordinator.cs"
    },
    @{
        Source = "BlazorShell\Infrastructure\Services\ModuleRouteProvider.cs"
        Dest = "2_Infrastructure\BlazorShell.ModuleSystem\Services\ModuleRouteProvider.cs"
    },
    @{
        Source = "BlazorShell\Infrastructure\Services\ModuleServiceManager.cs"
        Dest = "2_Infrastructure\BlazorShell.ModuleSystem\Services\ModuleServiceManager.cs"
    },
    @{
        Source = "BlazorShell\Infrastructure\Services\ModuleTemplateGenerator.cs"
        Dest = "2_Infrastructure\BlazorShell.ModuleSystem\Services\ModuleTemplateGenerator.cs"
    },
    @{
        Source = "BlazorShell\Infrastructure\Services\LazyModuleLoader.cs"
        Dest = "2_Infrastructure\BlazorShell.ModuleSystem\Services\LazyModuleLoader.cs"
    }
)

# General Infrastructure Services
$infraServices = @(
    @{
        Source = "BlazorShell\Infrastructure\Services\Services.cs"
        Dest = "2_Infrastructure\BlazorShell.Infrastructure\Services\Services.cs"
    }
)

# Move all infrastructure files
foreach ($file in ($dataFiles + $securityFiles + $moduleSystemFiles + $infraServices)) {
    $sourcePath = Join-Path $SolutionPath $file.Source
    $destPath = Join-Path $SolutionPath $file.Dest
    Move-Or-Copy-File -Source $sourcePath -Destination $destPath
}

# =======================
# STEP 4: MOVE APPLICATION SERVICES
# =======================
Write-Status "Step 4: Moving Application Services" "Info"

$appServices = @(
    @{
        Source = "BlazorShell\Core\Services\AuthenticationService.cs"
        Dest = "1_Core\BlazorShell.Application\Services\AuthenticationService.cs"
    },
    @{
        Source = "BlazorShell\Core\Services\ModuleCleanupService.cs"
        Dest = "1_Core\BlazorShell.Application\Services\ModuleCleanupService.cs"
    },
    @{
        Source = "BlazorShell\Core\Services\Interfaces.cs"
        Dest = "1_Core\BlazorShell.Application\Interfaces\ServiceInterfaces.cs"
    }
)

foreach ($file in $appServices) {
    $sourcePath = Join-Path $SolutionPath $file.Source
    $destPath = Join-Path $SolutionPath $file.Dest
    Move-Or-Copy-File -Source $sourcePath -Destination $destPath
}

# =======================
# STEP 5: MOVE WEB/PRESENTATION FILES
# =======================
Write-Status "Step 5: Moving Web Presentation files" "Info"

# Move main web files
$webFiles = @(
    @{
        Source = "BlazorShell\Program.cs"
        Dest = "3_Presentation\BlazorShell.Web\Program.cs"
    },
    @{
        Source = "BlazorShell\appsettings.json"
        Dest = "3_Presentation\BlazorShell.Web\appsettings.json"
    },
    @{
        Source = "BlazorShell\appsettings.Development.json"
        Dest = "3_Presentation\BlazorShell.Web\appsettings.Development.json"
    },
    @{
        Source = "BlazorShell\modules.json"
        Dest = "3_Presentation\BlazorShell.Web\modules.json"
    }
)

foreach ($file in $webFiles) {
    $sourcePath = Join-Path $SolutionPath $file.Source
    $destPath = Join-Path $SolutionPath $file.Dest
    Move-Or-Copy-File -Source $sourcePath -Destination $destPath -Copy
}

# Move Components
Write-Status "  Moving Components..." "Info"
$componentFiles = @(
    "App.razor",
    "Routes.razor",
    "_Imports.razor",
    "DynamicRouteHandler.razor",
    "DynamicRouteRefresher.cs",
    "ModuleRouter.razor",
    "ModuleServiceScope.cs"
)

foreach ($file in $componentFiles) {
    $sourcePath = Join-Path $SolutionPath "BlazorShell\Components\$file"
    $destPath = Join-Path $SolutionPath "3_Presentation\BlazorShell.Web\Components\$file"
    Move-Or-Copy-File -Source $sourcePath -Destination $destPath
}

# Move shared components to SharedUI
$sharedComponents = @(
    @{
        Source = "BlazorShell\Components\ModuleComponentBase.cs"
        Dest = "3_Presentation\BlazorShell.SharedUI\Components\ModuleComponentBase.cs"
    }
)

foreach ($file in $sharedComponents) {
    $sourcePath = Join-Path $SolutionPath $file.Source
    $destPath = Join-Path $SolutionPath $file.Dest
    Move-Or-Copy-File -Source $sourcePath -Destination $destPath
}

# Move Layout files
Write-Status "  Moving Layout files..." "Info"
$layoutPath = Join-Path $SolutionPath "BlazorShell\Components\Layout"
$destLayoutPath = Join-Path $SolutionPath "3_Presentation\BlazorShell.Web\Components\Layout"

if (Test-Path $layoutPath) {
    Get-ChildItem $layoutPath -File | ForEach-Object {
        Move-Or-Copy-File -Source $_.FullName -Destination (Join-Path $destLayoutPath $_.Name)
    }
}

# Move Pages
Write-Status "  Moving Pages..." "Info"
$pagesPath = Join-Path $SolutionPath "BlazorShell\Components\Pages"
$destPagesPath = Join-Path $SolutionPath "3_Presentation\BlazorShell.Web\Pages"

if (Test-Path $pagesPath) {
    Get-ChildItem $pagesPath -File | ForEach-Object {
        Move-Or-Copy-File -Source $_.FullName -Destination (Join-Path $destPagesPath $_.Name)
    }
}

# Move Account pages
Write-Status "  Moving Account pages..." "Info"
$accountPath = Join-Path $SolutionPath "BlazorShell\Components\Account"
$destAccountPath = Join-Path $SolutionPath "3_Presentation\BlazorShell.Web\Components\Account"

if (Test-Path $accountPath) {
    # Copy entire Account folder structure
    if (-not $DryRun) {
        if (-not (Test-Path $destAccountPath)) {
            Copy-Item -Path $accountPath -Destination $destAccountPath -Recurse -Force
            Write-Host "    Copied Account folder structure" -ForegroundColor Green
        }
    }
}

# Move wwwroot
Write-Status "  Moving wwwroot files..." "Info"
$wwwrootPath = Join-Path $SolutionPath "BlazorShell\wwwroot"
$destWwwrootPath = Join-Path $SolutionPath "3_Presentation\BlazorShell.Web\wwwroot"

if (Test-Path $wwwrootPath) {
    if (-not $DryRun) {
        if (-not (Test-Path $destWwwrootPath)) {
            Copy-Item -Path $wwwrootPath -Destination $destWwwrootPath -Recurse -Force
            Write-Host "    Copied wwwroot folder" -ForegroundColor Green
        }
    }
}

# =======================
# STEP 6: CREATE MODULE SDK BASE CLASSES
# =======================
Write-Status "Step 6: Creating Module SDK base classes" "Info"

$moduleBasePath = Join-Path $SolutionPath "4_ModuleSDK\BlazorShell.ModuleSDK\Base"
Ensure-Directory -Path $moduleBasePath

# Create ModuleBase.cs
$moduleBaseContent = @'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BlazorShell.Application.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorShell.ModuleSDK.Base
{
    public abstract class ModuleBase : IModule
    {
        protected ILogger? Logger { get; private set; }
        protected IServiceProvider? ServiceProvider { get; private set; }

        public abstract string Name { get; }
        public abstract string DisplayName { get; }
        public abstract string Description { get; }
        public virtual string Version => "1.0.0";
        public virtual string Author => "Unknown";
        public virtual string Icon => "bi bi-puzzle";
        public virtual string Category => "General";
        public virtual int Order => 100;

        public virtual async Task<bool> InitializeAsync(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            Logger = serviceProvider.GetService<ILogger>();
            Logger?.LogInformation($"Initializing module: {Name}");
            return await OnInitializeAsync();
        }

        public virtual async Task<bool> ActivateAsync()
        {
            Logger?.LogInformation($"Activating module: {Name}");
            return await OnActivateAsync();
        }

        public virtual async Task<bool> DeactivateAsync()
        {
            Logger?.LogInformation($"Deactivating module: {Name}");
            return await OnDeactivateAsync();
        }

        public virtual IEnumerable<NavigationItem> GetNavigationItems()
        {
            return Array.Empty<NavigationItem>();
        }

        public virtual IEnumerable<Type> GetComponentTypes()
        {
            return GetType().Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(ComponentBase)) && !t.IsAbstract);
        }

        public virtual Dictionary<string, object> GetDefaultSettings()
        {
            return new Dictionary<string, object>();
        }

        protected virtual Task<bool> OnInitializeAsync() => Task.FromResult(true);
        protected virtual Task<bool> OnActivateAsync() => Task.FromResult(true);
        protected virtual Task<bool> OnDeactivateAsync() => Task.FromResult(true);
    }
    
    public class NavigationItem
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}
'@

if (-not $DryRun) {
    $moduleBaseFile = Join-Path $moduleBasePath "ModuleBase.cs"
    Set-Content -Path $moduleBaseFile -Value $moduleBaseContent
    Write-Host "    Created ModuleBase.cs" -ForegroundColor Green
}

# =======================
# STEP 7: CREATE DEPENDENCY INJECTION FILES
# =======================
Write-Status "Step 7: Creating DependencyInjection extension methods" "Info"

# Application DI
$appDIContent = @'
using Microsoft.Extensions.DependencyInjection;
using BlazorShell.Application.Services;

namespace BlazorShell.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<AuthenticationService>();
            services.AddScoped<ModuleCleanupService>();
            return services;
        }
    }
}
'@

# Infrastructure DI
$infraDIContent = @'
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BlazorShell.Infrastructure.Data;
using BlazorShell.Domain.Entities;

namespace BlazorShell.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            
            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            
            return services;
        }
    }
}
'@

# ModuleSystem DI
$moduleDIContent = @'
using Microsoft.Extensions.DependencyInjection;
using BlazorShell.Application.Interfaces;
using BlazorShell.ModuleSystem.Loader;
using BlazorShell.ModuleSystem.Registry;
using BlazorShell.ModuleSystem.Services;

namespace BlazorShell.ModuleSystem
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddModuleSystem(this IServiceCollection services)
        {
            services.AddSingleton<IModuleLoader, ModuleLoader>();
            services.AddSingleton<IModuleRegistry, ModuleRegistry>();
            services.AddSingleton<IPluginAssemblyLoader, PluginAssemblyLoader>();
            services.AddSingleton<IDynamicRouteService, DynamicRouteService>();
            services.AddSingleton<IModuleServiceProvider, ModuleServiceProvider>();
            
            return services;
        }
    }
}
'@

if (-not $DryRun) {
    # Create Application DI
    $appDIPath = Join-Path $SolutionPath "1_Core\BlazorShell.Application\DependencyInjection.cs"
    Set-Content -Path $appDIPath -Value $appDIContent
    Write-Host "    Created Application DependencyInjection.cs" -ForegroundColor Green
    
    # Create Infrastructure DI
    $infraDIPath = Join-Path $SolutionPath "2_Infrastructure\BlazorShell.Infrastructure\DependencyInjection.cs"
    Set-Content -Path $infraDIPath -Value $infraDIContent
    Write-Host "    Created Infrastructure DependencyInjection.cs" -ForegroundColor Green
    
    # Create ModuleSystem DI
    $moduleDIPath = Join-Path $SolutionPath "2_Infrastructure\BlazorShell.ModuleSystem\DependencyInjection.cs"
    Set-Content -Path $moduleDIPath -Value $moduleDIContent
    Write-Host "    Created ModuleSystem DependencyInjection.cs" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "     File Migration Complete!          " -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

if ($DryRun) {
    Write-Status "This was a DRY RUN. Run without -DryRun to actually move files." "Warning"
} else {
    Write-Status "Next steps:" "Info"
    Write-Host "1. Update namespace references in all moved files"
    Write-Host "2. Update module references (remove BlazorShell, add ModuleSDK)"
    Write-Host "3. Fix any missing using statements"
    Write-Host "4. Run 'dotnet build' to identify compilation errors"
}