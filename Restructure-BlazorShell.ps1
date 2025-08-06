# Simple-Solution-Update.ps1
# A simpler script to update your solution file

$SolutionPath = "D:\temp\_myProj\BlazorShell"
Set-Location $SolutionPath

Write-Host "Updating BlazorShell Solution..." -ForegroundColor Cyan

# Find solution file
$slnFile = Get-ChildItem -Filter "*.sln" | Select-Object -First 1
if (-not $slnFile) {
    Write-Host "No solution file found!" -ForegroundColor Red
    exit
}

Write-Host "Found: $($slnFile.Name)" -ForegroundColor Green

# Backup
$backup = "$($slnFile.FullName).backup"
Copy-Item $slnFile.FullName $backup
Write-Host "Backup created: $backup" -ForegroundColor Yellow

# Remove old project
Write-Host "Removing old projects..." -ForegroundColor Yellow
dotnet sln remove "BlazorShell\BlazorShell.csproj" 2>$null

# Add new projects
Write-Host "Adding new projects..." -ForegroundColor Yellow

$projects = @(
    "1_Core\BlazorShell.Domain\BlazorShell.Domain.csproj",
    "1_Core\BlazorShell.Application\BlazorShell.Application.csproj",
    "2_Infrastructure\BlazorShell.Infrastructure\BlazorShell.Infrastructure.csproj",
    "2_Infrastructure\BlazorShell.ModuleSystem\BlazorShell.ModuleSystem.csproj",
    "3_Presentation\BlazorShell.Web\BlazorShell.Web.csproj",
    "3_Presentation\BlazorShell.SharedUI\BlazorShell.SharedUI.csproj",
    "4_ModuleSDK\BlazorShell.ModuleSDK\BlazorShell.ModuleSDK.csproj",
    "5_Modules\BlazorShell.Modules.Dashboard\BlazorShell.Modules.Dashboard.csproj",
    "5_Modules\BlazorShell.Modules.Admin\BlazorShell.Modules.Admin.csproj"
)

foreach ($proj in $projects) {
    if (Test-Path $proj) {
        dotnet sln add $proj 2>$null
        Write-Host "  Added: $(Split-Path $proj -Leaf)" -ForegroundColor Green
    } else {
        Write-Host "  Missing: $proj" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Solution updated!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Open in Visual Studio"
Write-Host "2. Set BlazorShell.Web as startup project"
Write-Host "3. Build -> Rebuild Solution"