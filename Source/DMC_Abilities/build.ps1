Write-Host "DMC Abilities Build Script" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan

# Check if Lib folder exists
if (-not (Test-Path ".\Lib")) {
    Write-Host ""
    Write-Host "ERROR: Lib folder not found!" -ForegroundColor Red
    Write-Host "Please copy the required RimWorld DLLs to a 'Lib' folder:" -ForegroundColor Yellow
    Write-Host "- Assembly-CSharp.dll"
    Write-Host "- 0Harmony.dll"
    Write-Host "- Unity.TextMeshPro.dll"
    Write-Host "- UnityEngine.CoreModule.dll"
    Write-Host "- UnityEngine.IMGUIModule.dll"
    Write-Host ""
    Write-Host "You can find these in:"
    Write-Host "- RimWorld\RimWorldWin64_Data\Managed\ (for Unity DLLs and Assembly-CSharp)"
    Write-Host "- RimWorld\Mods\[ModWithHarmony]\Assemblies\ (for 0Harmony.dll)"
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "Building mod..." -ForegroundColor Yellow
& dotnet build -p:RimWorldPath=".\Lib"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "BUILD SUCCESSFUL!" -ForegroundColor Green
    Write-Host "Compiled DLL: ..\..\Assemblies\DMCAbilities.dll" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "BUILD FAILED!" -ForegroundColor Red
}

Write-Host ""
Read-Host "Press Enter to exit"