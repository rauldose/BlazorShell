# Fix-ProjectReferences.ps1
# Run this script after the main restructuring to fix any reference issues

param(
    [string]$SolutionPath = (Get-Location).Path
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

# Define the correct project structure and dependencies
$projectDependencies = @{
    "1_Core\BlazorShell.Domain" = @()
    
    "1_Core\BlazorShell.Application" = @(
        "1_Core\BlazorShell.Domain"
    )
    
    "2_Infrastructure\BlazorShell.Infrastructure" = @(
        "1_Core\BlazorShell.Application",
        "1_Core\BlazorShell.Domain"
    )
    
    "2_Infrastructure\BlazorShell.ModuleSystem" = @(
        "1_Core\BlazorShell.Application",
        "1_Core\BlazorShell.Domain"
    )
    
    "3_Presentation\BlazorShell.SharedUI" = @(
        "1_Core\BlazorShell.Application",
        "1_Core\BlazorShell.Domain"
    )
    
    "3_Presentation\BlazorShell.Web" = @(
        "1_Core\BlazorShell.Application",
        "1_Core\BlazorShell.Domain",
        "2_Infrastructure\BlazorShell.Infrastructure",
        "2_Infrastructure\BlazorShell.ModuleSystem",
        "3_Presentation\BlazorShell.SharedUI"
    )
    
    "4_ModuleSDK\BlazorShell.ModuleSDK" = @(
        "1_Core\BlazorShell.Application",
        "1_Core\BlazorShell.Domain"
    )
    
    "5_Modules\BlazorShell.Modules.Dashboard" = @(
        "4_ModuleSDK\BlazorShell.ModuleSDK",
        "3_Presentation\BlazorShell.SharedUI"
    )
    
    "5_Modules\BlazorShell.Modules.Admin" = @(
        "4_ModuleSDK\BlazorShell.ModuleSDK",
        "3_Presentation\BlazorShell.SharedUI"
    )
}

# NuGet packages for each project
$projectPackages = @{
    "2_Infrastructure\BlazorShell.Infrastructure" = @(
        "Microsoft.EntityFrameworkCore.SqlServer",
        "Microsoft.AspNetCore.Identity.EntityFrameworkCore",
        "Microsoft.AspNetCore.DataProtection.EntityFrameworkCore",
        "Newtonsoft.Json"
    )
    
    "3_Presentation\BlazorShell.Web" = @(
        "Microsoft.AspNetCore.Components.Authorization",
        "Microsoft.AspNetCore.Identity.UI",
        "Microsoft.EntityFrameworkCore.Tools",
        "Autofac",
        "Autofac.Extensions.DependencyInjection"
    )
    
    "3_Presentation\BlazorShell.SharedUI" = @(
        "Microsoft.AspNetCore.Components.Web"
    )
    
    "4_ModuleSDK\BlazorShell.ModuleSDK" = @(
        "Microsoft.Extensions.DependencyInjection.Abstractions",
        "Microsoft.Extensions.Logging.Abstractions"
    )
}

function Fix-ProjectFile {
    param(
        [string]$ProjectPath,
        [string[]]$Dependencies,
        [string[]]$Packages
    )
    
    $projectName = Split-Path $ProjectPath -Leaf
    $projectFile = Join-Path $SolutionPath "$ProjectPath\$projectName.csproj"
    
    if (-not (Test-Path $projectFile)) {
        Write-Status "Creating missing project: $projectName" "Warning"
        
        # Determine project type
        $projectType = if ($projectName -like "*Web*") { 
            "web" 
        } elseif ($projectName -like "*SharedUI*" -or $projectName -like "*Modules.*") { 
            "razorclasslib" 
        } else { 
            "classlib" 
        }
        
        # Create the project
        $projectDir = Split-Path $projectFile -Parent
        if (-not (Test-Path $projectDir)) {
            New-Item -ItemType Directory -Path $projectDir -Force | Out-Null
        }
        
        Push-Location $projectDir
        try {
            if ($projectType -eq "web") {
                dotnet new blazor -n $projectName -f net8.0 --interactivity Server --force
            } else {
                dotnet new $projectType -n $projectName -f net8.0 --force
            }
            Write-Status "  Created $projectName" "Success"
        }
        finally {
            Pop-Location
        }
    }
    
    if (Test-Path $projectFile) {
        Write-Status "Processing: $projectName"
        
        # Clear existing references (optional - comment out if you want to keep existing)
        # $content = Get-Content $projectFile -Raw
        # $content = $content -replace '<ProjectReference[^>]*>', ''
        # Set-Content $projectFile $content
        
        # Add dependencies
        foreach ($dep in $Dependencies) {
            $depName = Split-Path $dep -Leaf
            $depProjectFile = Join-Path $SolutionPath "$dep\$depName.csproj"
            
            if (Test-Path $depProjectFile) {
                $relativePath = Resolve-Path -Path $depProjectFile -Relative -RelativeBasePath (Split-Path $projectFile -Parent)
                
                try {
                    dotnet add $projectFile reference $depProjectFile 2>&1 | Out-Null
                    Write-Status "  Added reference: $depName" "Success"
                }
                catch {
                    Write-Status "  Failed to add reference: $depName" "Warning"
                }
            } else {
                Write-Status "  Dependency not found: $depName" "Warning"
            }
        }
        
        # Add NuGet packages
        foreach ($package in $Packages) {
            try {
                dotnet add $projectFile package $package 2>&1 | Out-Null
                Write-Status "  Added package: $package" "Success"
            }
            catch {
                Write-Status "  Package might already exist: $package" "Warning"
            }
        }
    }
}

function Main {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "   Fix BlazorShell Project References  " -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    
    # Check if we're in the right directory
    if (-not (Test-Path (Join-Path $SolutionPath "*.sln"))) {
        Write-Status "No solution file found. Are you in the right directory?" "Error"
        return
    }
    
    # Fix each project
    foreach ($project in $projectDependencies.Keys) {
        $packages = if ($projectPackages.ContainsKey($project)) { 
            $projectPackages[$project] 
        } else { 
            @() 
        }
        
        Fix-ProjectFile -ProjectPath $project `
                       -Dependencies $projectDependencies[$project] `
                       -Packages $packages
        
        Write-Host ""
    }
    
    # Update solution file
    Write-Status "Updating solution file..."
    $solutionFile = Get-ChildItem -Path $SolutionPath -Filter "*.sln" | Select-Object -First 1
    
    foreach ($projectPath in $projectDependencies.Keys) {
        $projectName = Split-Path $projectPath -Leaf
        $csprojPath = "$projectPath\$projectName.csproj"
        
        if (Test-Path (Join-Path $SolutionPath $csprojPath)) {
            try {
                dotnet sln $solutionFile.FullName add $csprojPath 2>&1 | Out-Null
                Write-Status "  Added to solution: $projectName" "Success"
            }
            catch {
                # Project might already be in solution
            }
        }
    }
    
    Write-Host ""
    Write-Status "Building solution to verify..."
    dotnet build --no-restore 2>&1 | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Status "Solution builds successfully!" "Success"
    } else {
        Write-Status "Build failed. Run 'dotnet build' to see detailed errors" "Warning"
        Write-Status "You may need to:" "Info"
        Write-Host "  1. Update namespace references in .cs files"
        Write-Host "  2. Move remaining files to appropriate projects"
        Write-Host "  3. Fix any remaining compilation errors"
    }
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "         Reference Fix Complete        " -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
}

# Run the main function
Main