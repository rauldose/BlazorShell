# BlazorShell Code Cleanup and File Splitting Script
# This script splits large files, removes duplicates, and organizes code
# Run after the main restructuring script

param(
    [string]$SolutionPath = (Get-Location).Path,
    [switch]$DryRun = $false
)

$ErrorActionPreference = "Stop"

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

# File splitting configurations
$fileSplitConfigs = @{
    "Entities.cs" = @{
        OutputPath = "1_Core\BlazorShell.Domain\Entities"
        SplitBy = "class"
        Files = @{
            "ApplicationUser.cs" = "ApplicationUser"
            "ApplicationRole.cs" = "ApplicationRole"
            "Module.cs" = "Module"
            "ModulePermission.cs" = "ModulePermission"
            "NavigationItem.cs" = "NavigationItem"
            "AuditLog.cs" = "AuditLog"
            "Setting.cs" = "Setting"
        }
    }
    "Interfaces.cs" = @{
        OutputPath = "1_Core\BlazorShell.Application\Interfaces"
        SplitBy = "interface"
        Files = @{
            "IModule.cs" = "IModule"
            "IServiceModule.cs" = "IServiceModule"
            "IConfigurableModule.cs" = "IConfigurableModule"
            "IModuleLoader.cs" = "IModuleLoader"
            "IModuleRegistry.cs" = "IModuleRegistry"
            "IPluginAssemblyLoader.cs" = "IPluginAssemblyLoader"
            "IDynamicRouteService.cs" = "IDynamicRouteService"
            "IModuleAuthorizationService.cs" = "IModuleAuthorizationService"
            "INavigationService.cs" = "INavigationService"
            "IStateContainer.cs" = "IStateContainer"
        }
    }
    "ModuleLoader.cs" = @{
        OutputPath = "2_Infrastructure\BlazorShell.ModuleSystem\Loader"
        SplitBy = "class"
        Files = @{
            "ModuleLoader.cs" = "ModuleLoader"
            "ModuleLoadContext.cs" = "ModuleLoadContext"
            "ModulesConfiguration.cs" = "ModulesConfiguration|ModuleSettings|ModuleConfig"
            "NavigationItemConfig.cs" = "NavigationItemConfig"
            "PermissionConfig.cs" = "PermissionConfig"
        }
    }
    "ModuleManagementService.cs" = @{
        OutputPath = "2_Infrastructure\BlazorShell.ModuleSystem\Services"
        SplitBy = "class"
        Files = @{
            "ModuleManagementService.cs" = "ModuleManagementService"
            "IModuleManagementService.cs" = "IModuleManagementService"
            "ModuleInfo.cs" = "ModuleInfo"
            "ModuleOperationResult.cs" = "ModuleOperationResult"
            "ModuleUploadResult.cs" = "ModuleUploadResult"
            "ModuleDependency.cs" = "ModuleDependency"
            "ModuleHealthStatus.cs" = "ModuleHealthStatus"
        }
    }
}

function Split-LargeFile {
    param(
        [string]$FilePath,
        [hashtable]$Config
    )
    
    if (-not (Test-Path $FilePath)) {
        Write-Status "File not found: $FilePath" "Warning"
        return
    }
    
    Write-Status "Splitting file: $(Split-Path $FilePath -Leaf)"
    
    $content = Get-Content $FilePath -Raw
    $lines = $content -split "`n"
    
    # Extract usings
    $usings = @()
    $namespaceStart = -1
    $currentNamespace = ""
    
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "^using\s+") {
            $usings += $lines[$i]
        }
        if ($lines[$i] -match "^namespace\s+(.+)") {
            $currentNamespace = $matches[1].Trim()
            $namespaceStart = $i
            break
        }
    }
    
    foreach ($outputFile in $Config.Files.Keys) {
        $patterns = $Config.Files[$outputFile] -split "\|"
        $outputPath = Join-Path $SolutionPath $Config.OutputPath $outputFile
        
        Write-Status "  Creating: $outputFile"
        
        if (-not $DryRun) {
            $outputDir = Split-Path $outputPath -Parent
            if (-not (Test-Path $outputDir)) {
                New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
            }
            
            # Build the new file content
            $newContent = @()
            $newContent += $usings
            $newContent += ""
            $newContent += "namespace $currentNamespace"
            $newContent += "{"
            
            # Extract relevant classes/interfaces
            foreach ($pattern in $patterns) {
                $inTarget = $false
                $braceCount = 0
                $targetContent = @()
                
                for ($i = $namespaceStart + 1; $i -lt $lines.Count; $i++) {
                    if ($lines[$i] -match "public\s+(class|interface|enum)\s+$pattern") {
                        $inTarget = $true
                        $braceCount = 0
                    }
                    
                    if ($inTarget) {
                        $targetContent += $lines[$i]
                        
                        # Count braces
                        $braceCount += ($lines[$i] -split "{").Count - 1
                        $braceCount -= ($lines[$i] -split "}").Count - 1
                        
                        if ($braceCount -eq 0 -and $targetContent.Count -gt 1) {
                            $inTarget = $false
                            $newContent += $targetContent
                            $newContent += ""
                            break
                        }
                    }
                }
            }
            
            $newContent += "}"
            
            Set-Content -Path $outputPath -Value ($newContent -join "`n")
        }
    }
}

function Remove-DuplicateCode {
    Write-Status "Scanning for duplicate code patterns..."
    
    $codePatterns = @{}
    $files = Get-ChildItem -Path $SolutionPath -Include "*.cs" -Recurse
    
    foreach ($file in $files) {
        $content = Get-Content $file.FullName -Raw
        
        # Extract method signatures
        $methods = [regex]::Matches($content, '(public|private|protected|internal)\s+[\w<>]+\s+\w+\s*\([^)]*\)')
        
        foreach ($method in $methods) {
            $signature = $method.Value
            if (-not $codePatterns.ContainsKey($signature)) {
                $codePatterns[$signature] = @()
            }
            $codePatterns[$signature] += $file.FullName
        }
    }
    
    # Report duplicates
    $duplicates = $codePatterns.GetEnumerator() | Where-Object { $_.Value.Count -gt 1 }
    
    if ($duplicates) {
        Write-Status "Found duplicate code patterns:" "Warning"
        foreach ($dup in $duplicates | Select-Object -First 10) {
            Write-Host "  - $($dup.Key.Substring(0, [Math]::Min(50, $dup.Key.Length)))..." -ForegroundColor Yellow
            foreach ($file in $dup.Value | Select-Object -First 3) {
                Write-Host "    in $(Split-Path $file -Leaf)" -ForegroundColor Gray
            }
        }
    } else {
        Write-Status "No significant duplicates found" "Success"
    }
}

function Create-ModuleSDK {
    Write-Status "Creating Module SDK base classes..."
    
    $sdkPath = Join-Path $SolutionPath "4_ModuleSDK\BlazorShell.ModuleSDK"
    
    $baseModuleContent = @'
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorShell.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorShell.ModuleSDK.Base
{
    /// <summary>
    /// Base class for all modules, providing common functionality
    /// </summary>
    public abstract class ModuleBase : IModule
    {
        protected ILogger? Logger { get; private set; }
        protected IServiceProvider? ServiceProvider { get; private set; }
        protected Dictionary<string, object> Configuration { get; private set; } = new();

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

        // Protected methods for derived classes to override
        protected virtual Task<bool> OnInitializeAsync() => Task.FromResult(true);
        protected virtual Task<bool> OnActivateAsync() => Task.FromResult(true);
        protected virtual Task<bool> OnDeactivateAsync() => Task.FromResult(true);
    }
}
'@

    $moduleComponentBaseContent = @'
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BlazorShell.ModuleSDK.Base
{
    /// <summary>
    /// Base component for module components with common functionality
    /// </summary>
    public abstract class ModuleComponentBase : ComponentBase
    {
        [Inject] protected NavigationManager Navigation { get; set; } = default!;
        [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

        protected ClaimsPrincipal? User { get; private set; }
        protected bool IsAuthenticated { get; private set; }

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            User = authState.User;
            IsAuthenticated = authState.User?.Identity?.IsAuthenticated ?? false;
            
            await base.OnInitializedAsync();
        }
    }
}
'@

    if (-not $DryRun) {
        $basePath = Join-Path $sdkPath "Base"
        if (-not (Test-Path $basePath)) {
            New-Item -ItemType Directory -Path $basePath -Force | Out-Null
        }
        
        Set-Content -Path (Join-Path $basePath "ModuleBase.cs") -Value $baseModuleContent
        Set-Content -Path (Join-Path $basePath "ModuleComponentBase.cs") -Value $moduleComponentBaseContent
        
        Write-Status "Created Module SDK base classes" "Success"
    }
}

function Update-ModuleReferences {
    Write-Status "Updating module project references..."
    
    $modulePaths = @(
        "5_Modules\BlazorShell.Modules.Dashboard",
        "5_Modules\BlazorShell.Modules.Admin"
    )
    
    foreach ($modulePath in $modulePaths) {
        $fullPath = Join-Path $SolutionPath $modulePath
        $projectFile = Join-Path $fullPath "$(Split-Path $modulePath -Leaf).csproj"
        
        if (Test-Path $projectFile) {
            Write-Status "  Updating: $(Split-Path $modulePath -Leaf)"
            
            if (-not $DryRun) {
                # Remove old reference to main app
                $content = Get-Content $projectFile -Raw
                $content = $content -replace '<ProjectReference Include="[^"]*BlazorShell\.csproj"[^>]*>', ''
                
                # Add new references
                $newRefs = @"
    <ProjectReference Include="..\..\4_ModuleSDK\BlazorShell.ModuleSDK\BlazorShell.ModuleSDK.csproj" />
    <ProjectReference Include="..\..\3_Presentation\BlazorShell.SharedUI\BlazorShell.SharedUI.csproj" />
"@
                
                $content = $content -replace '(</ItemGroup>)', "$newRefs`n  `$1"
                Set-Content -Path $projectFile -Value $content
            }
        }
    }
    
    Write-Status "Module references updated" "Success"
}

function Main {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "   BlazorShell Code Cleanup Script    " -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    
    if ($DryRun) {
        Write-Status "Running in DRY RUN mode - no changes will be made" "Warning"
    }
    
    # Split large files
    Write-Status "Splitting large files..."
    foreach ($file in $fileSplitConfigs.Keys) {
        $filePath = Join-Path $SolutionPath "BlazorShell" $file
        if (Test-Path $filePath) {
            Split-LargeFile -FilePath $filePath -Config $fileSplitConfigs[$file]
        }
    }
    
    Write-Host ""
    
    # Remove duplicate code
    Remove-DuplicateCode
    
    Write-Host ""
    
    # Create Module SDK
    Create-ModuleSDK
    
    Write-Host ""
    
    # Update module references
    Update-ModuleReferences
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "        Cleanup Complete!              " -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    
    if ($DryRun) {
        Write-Host ""
        Write-Status "This was a DRY RUN. Run without -DryRun flag to apply changes." "Warning"
    }
}

# Run the main function
Main