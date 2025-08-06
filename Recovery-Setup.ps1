# Recovery-Setup.ps1
# This script helps recover from partial migration and sets up the structure manually

param(
    [string]$SolutionPath = "D:\temp\_myProj\BlazorShell",
    [switch]$Force = $false
)

$ErrorActionPreference = "Continue"  # Continue even if some operations fail

function Write-Status {
    param([string]$Message, [string]$Type = "Info")
    $color = switch($Type) {
        "Success" { "Green" }
        "Warning" { "Yellow" }
        "Error" { "Red" }
        "Step" { "Magenta" }
        default { "Cyan" }
    }
    Write-Host "[$Type] $Message" -ForegroundColor $color
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "    BlazorShell Recovery & Setup       " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Verify current state
Write-Status "Step 1: Checking current directory structure" "Step"
$folders = @(
    "1_Core\BlazorShell.Domain",
    "1_Core\BlazorShell.Application",
    "2_Infrastructure\BlazorShell.Infrastructure",
    "2_Infrastructure\BlazorShell.ModuleSystem",
    "3_Presentation\BlazorShell.Web",
    "3_Presentation\BlazorShell.SharedUI",
    "4_ModuleSDK\BlazorShell.ModuleSDK",
    "5_Modules\BlazorShell.Modules.Dashboard",
    "5_Modules\BlazorShell.Modules.Admin"
)

$missingProjects = @()
foreach ($folder in $folders) {
    $projectName = Split-Path $folder -Leaf
    $projectFile = Join-Path $SolutionPath "$folder\$projectName.csproj"
    
    if (Test-Path $projectFile) {
        Write-Status "  Found: $projectName" "Success"
    } else {
        Write-Status "  Missing: $projectName" "Warning"
        $missingProjects += $folder
    }
}

if ($missingProjects.Count -eq 0) {
    Write-Status "All projects exist!" "Success"
} else {
    Write-Status "Found $($missingProjects.Count) missing projects" "Warning"
    
    $create = if ($Force) { 'y' } else { Read-Host "Do you want to create the missing projects? (y/n)" }
    
    if ($create -eq 'y') {
        Write-Host ""
        Write-Status "Step 2: Creating missing projects" "Step"
        
        foreach ($projectPath in $missingProjects) {
            $projectName = Split-Path $projectPath -Leaf
            $fullPath = Join-Path $SolutionPath $projectPath
            
            Write-Status "Creating $projectName..." "Info"
            
            # Create directory
            if (-not (Test-Path $fullPath)) {
                New-Item -ItemType Directory -Path $fullPath -Force | Out-Null
            }
            
            # Determine project type
            $template = switch -Wildcard ($projectName) {
                "*Web*" { "blazor" }
                "*SharedUI*" { "razorclasslib" }
                "*Modules.*" { "razorclasslib" }
                default { "classlib" }
            }
            
            # Create project
            Push-Location $fullPath
            try {
                if ($template -eq "blazor") {
                    # For Blazor Web App
                    dotnet new blazor -n $projectName -f net8.0 --interactivity Server --empty --force
                } else {
                    dotnet new $template -n $projectName -f net8.0 --force
                }
                Write-Status "  Created $projectName successfully" "Success"
            }
            catch {
                Write-Status "  Failed to create $projectName : $_" "Error"
            }
            finally {
                Pop-Location
            }
        }
    }
}

Write-Host ""
Write-Status "Step 3: Setting up project dependencies" "Step"

# Manual reference setup using dotnet CLI
$references = @{
    "1_Core\BlazorShell.Application" = @("1_Core\BlazorShell.Domain")
    "2_Infrastructure\BlazorShell.Infrastructure" = @("1_Core\BlazorShell.Application", "1_Core\BlazorShell.Domain")
    "2_Infrastructure\BlazorShell.ModuleSystem" = @("1_Core\BlazorShell.Application", "1_Core\BlazorShell.Domain")
    "3_Presentation\BlazorShell.SharedUI" = @("1_Core\BlazorShell.Application")
    "3_Presentation\BlazorShell.Web" = @(
        "1_Core\BlazorShell.Application",
        "2_Infrastructure\BlazorShell.Infrastructure",
        "2_Infrastructure\BlazorShell.ModuleSystem",
        "3_Presentation\BlazorShell.SharedUI"
    )
    "4_ModuleSDK\BlazorShell.ModuleSDK" = @("1_Core\BlazorShell.Application")
    "5_Modules\BlazorShell.Modules.Dashboard" = @("4_ModuleSDK\BlazorShell.ModuleSDK", "3_Presentation\BlazorShell.SharedUI")
    "5_Modules\BlazorShell.Modules.Admin" = @("4_ModuleSDK\BlazorShell.ModuleSDK", "3_Presentation\BlazorShell.SharedUI")
}

foreach ($project in $references.Keys) {
    $projectName = Split-Path $project -Leaf
    $projectFile = Join-Path $SolutionPath "$project\$projectName.csproj"
    
    if (Test-Path $projectFile) {
        Write-Status "Setting up references for $projectName" "Info"
        
        foreach ($ref in $references[$project]) {
            $refName = Split-Path $ref -Leaf
            $refFile = Join-Path $SolutionPath "$ref\$refName.csproj"
            
            if (Test-Path $refFile) {
                $result = dotnet add $projectFile reference $refFile 2>&1
                if ($LASTEXITCODE -eq 0) {
                    Write-Status "  Added reference to $refName" "Success"
                } else {
                    Write-Status "  Failed to add $refName" "Warning"
                }
            }
        }
    }
}

Write-Host ""
Write-Status "Step 4: Adding essential NuGet packages" "Step"

$packages = @{
    "2_Infrastructure\BlazorShell.Infrastructure" = @(
        "Microsoft.EntityFrameworkCore.SqlServer",
        "Microsoft.AspNetCore.Identity.EntityFrameworkCore"
    )
    "3_Presentation\BlazorShell.Web" = @(
        "Microsoft.AspNetCore.Components.Authorization",
        "Microsoft.EntityFrameworkCore.Tools"
    )
}

foreach ($project in $packages.Keys) {
    $projectName = Split-Path $project -Leaf
    $projectFile = Join-Path $SolutionPath "$project\$projectName.csproj"
    
    if (Test-Path $projectFile) {
        Write-Status "Adding packages to $projectName" "Info"
        
        foreach ($package in $packages[$project]) {
            $result = dotnet add $projectFile package $package 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Status "  Added $package" "Success"
            }
        }
    }
}

Write-Host ""
Write-Status "Step 5: Updating solution file" "Step"

$solutionFiles = Get-ChildItem -Path $SolutionPath -Filter "*.sln"
if ($solutionFiles.Count -gt 0) {
    $solutionFile = $solutionFiles[0].FullName
    Write-Status "Found solution: $($solutionFiles[0].Name)" "Info"
    
    foreach ($folder in $folders) {
        $projectName = Split-Path $folder -Leaf
        $csprojPath = "$folder\$projectName.csproj"
        $fullCsprojPath = Join-Path $SolutionPath $csprojPath
        
        if (Test-Path $fullCsprojPath) {
            $result = dotnet sln $solutionFile add $csprojPath 2>&1
            if ($result -like "*already*") {
                Write-Status "  $projectName already in solution" "Info"
            } elseif ($LASTEXITCODE -eq 0) {
                Write-Status "  Added $projectName to solution" "Success"
            }
        }
    }
}

Write-Host ""
Write-Status "Step 6: Creating basic folder structure" "Step"

$folderStructure = @{
    "1_Core\BlazorShell.Domain" = @("Entities", "ValueObjects", "Enums", "Interfaces")
    "1_Core\BlazorShell.Application" = @("Interfaces", "Services", "DTOs", "Mappings")
    "2_Infrastructure\BlazorShell.Infrastructure" = @("Data", "Security", "Services", "Repositories")
    "2_Infrastructure\BlazorShell.ModuleSystem" = @("Loader", "Registry", "Services", "Configuration")
    "3_Presentation\BlazorShell.Web" = @("Components", "Pages", "wwwroot", "Services")
    "3_Presentation\BlazorShell.SharedUI" = @("Components", "Layouts", "Shared")
    "4_ModuleSDK\BlazorShell.ModuleSDK" = @("Base", "Interfaces", "Attributes", "Extensions")
}

foreach ($project in $folderStructure.Keys) {
    foreach ($folder in $folderStructure[$project]) {
        $folderPath = Join-Path $SolutionPath "$project\$folder"
        if (-not (Test-Path $folderPath)) {
            New-Item -ItemType Directory -Path $folderPath -Force | Out-Null
            Write-Status "  Created folder: $project\$folder" "Success"
        }
    }
}

Write-Host ""
Write-Status "Step 7: Testing build" "Step"

Push-Location $SolutionPath
try {
    Write-Status "Running dotnet restore..." "Info"
    dotnet restore 2>&1 | Out-Null
    
    Write-Status "Running dotnet build..." "Info"
    $buildResult = dotnet build --no-restore 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Status "Solution builds successfully!" "Success"
    } else {
        Write-Status "Build failed - this is expected at this stage" "Warning"
        Write-Status "  You will need to:" "Info"
        Write-Host "    1. Move/copy code files to appropriate projects"
        Write-Host "    2. Update namespace references"
        Write-Host "    3. Fix any compilation errors"
    }
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "       Recovery Process Complete        " -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Status "Next Steps:" "Step"
Write-Host "1. Run the cleanup script to split and organize files"
Write-Host "2. Move existing code to the new project structure:"
Write-Host "   - Entities to 1_Core\BlazorShell.Domain\Entities"
Write-Host "   - Interfaces to 1_Core\BlazorShell.Application\Interfaces"
Write-Host "   - Infrastructure to 2_Infrastructure\BlazorShell.Infrastructure"
Write-Host "   - Module code to 2_Infrastructure\BlazorShell.ModuleSystem"
Write-Host "   - Components to 3_Presentation\BlazorShell.SharedUI\Components"
Write-Host "3. Update namespaces in all .cs files"
Write-Host "4. Fix any remaining build errors"
Write-Host ""
Write-Status "The solution structure is now ready for code migration!" "Success"