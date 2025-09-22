param(
    [string]$RimWorldPath = ""
)

Write-Host "DMC Abilities Build Script" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan
Write-Host ""

# Function to test if a path contains required RimWorld DLLs
function Test-RimWorldPath {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return $false }
    
    $requiredDlls = @(
        "Assembly-CSharp.dll",
        "Unity.TextMeshPro.dll",
        "UnityEngine.CoreModule.dll",
        "UnityEngine.IMGUIModule.dll"
    )
    
    foreach ($dll in $requiredDlls) {
        if (-not (Test-Path (Join-Path $Path $dll))) {
            return $false
        }
    }
    return $true
}

# Function to download 0Harmony.dll if missing
function Get-HarmonyDll {
    param([string]$LibPath)
    $harmonyPath = Join-Path $LibPath "0Harmony.dll"
    if (-not (Test-Path $harmonyPath)) {
        Write-Host "Downloading 0Harmony.dll..." -ForegroundColor Yellow
        try {
            $url = "https://github.com/pardeike/Harmony/releases/download/v2.2.2.0/0Harmony.dll"
            Invoke-WebRequest -Uri $url -OutFile $harmonyPath -UseBasicParsing
            Write-Host "✓ 0Harmony.dll downloaded successfully" -ForegroundColor Green
        } catch {
            Write-Host "Failed to download 0Harmony.dll. Please download it manually from:" -ForegroundColor Red
            Write-Host "https://github.com/pardeike/Harmony/releases/download/v2.2.2.0/0Harmony.dll" -ForegroundColor Red
            return $false
        }
    }
    return $true
}

# Try to find RimWorld installation automatically if not provided
if ([string]::IsNullOrEmpty($RimWorldPath)) {
    Write-Host "Searching for RimWorld installation..." -ForegroundColor Yellow
    
    # First check if we have a local Lib folder with the required DLLs
    $localLibPath = ".\Lib"
    if (Test-Path $localLibPath) {
        Write-Host "  Checking local Lib folder..." -ForegroundColor DarkGray
        $hasAllDlls = $true
        $requiredDlls = @(
            "Assembly-CSharp.dll",
            "Unity.TextMeshPro.dll", 
            "UnityEngine.CoreModule.dll",
            "UnityEngine.IMGUIModule.dll"
        )
        
        foreach ($dll in $requiredDlls) {
            if (-not (Test-Path (Join-Path $localLibPath $dll))) {
                $hasAllDlls = $false
                break
            }
        }
        
        # Download Harmony if missing and we have other DLLs
        if ($hasAllDlls) {
            if (Get-HarmonyDll $localLibPath) {
                $RimWorldPath = $localLibPath
                Write-Host "Found complete DLL set in local Lib folder" -ForegroundColor Green
            }
        }
    }
    
    # If local Lib doesn't have everything, search for RimWorld installation
    if ([string]::IsNullOrEmpty($RimWorldPath)) {
    
    # Comprehensive search paths for different installation types
    $searchPaths = @(
        # Standard Steam paths
        "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed",
        "D:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed",
        "E:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed",
        "F:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed",
        
        # Epic Games paths
        "C:\Program Files\Epic Games\RimWorld\RimWorldWin64_Data\Managed",
        "D:\Epic Games\RimWorld\RimWorldWin64_Data\Managed",
        
        # GOG paths
        "C:\GOG Games\RimWorld\RimWorldWin64_Data\Managed",
        "D:\GOG Games\RimWorld\RimWorldWin64_Data\Managed",
        
        # User's specific path
        "D:\Game\Rimworld\RimWorldWin64_Data\Managed",
        "D:\Game\RimWorld\RimWorldWin64_Data\Managed",
        "D:\Games\Rimworld\RimWorldWin64_Data\Managed",
        "D:\Games\RimWorld\RimWorldWin64_Data\Managed",
        
        # Common alternative paths
        "C:\Games\RimWorld\RimWorldWin64_Data\Managed",
        "E:\Games\RimWorld\RimWorldWin64_Data\Managed",
        "F:\Games\RimWorld\RimWorldWin64_Data\Managed",
        
        # Portable/custom installations
        "D:\RimWorld\RimWorldWin64_Data\Managed",
        "E:\RimWorld\RimWorldWin64_Data\Managed",
        "F:\RimWorld\RimWorldWin64_Data\Managed",
        
        # Local project folder fallback
        "..\..\Lib"
    )
    
    # Also do a smart search on common drives
    Write-Host "Performing deep search on drives C:, D:, E:, F:..." -ForegroundColor Gray
    $driveSearchPaths = @()
    
    foreach ($drive in @("C:", "D:", "E:", "F:")) {
        if (Test-Path $drive) {
            try {
                # Search for common RimWorld folder patterns
                $patterns = @(
                    "*RimWorld*\RimWorldWin64_Data\Managed",
                    "*rimworld*\RimWorldWin64_Data\Managed",
                    "*Rimworld*\RimWorldWin64_Data\Managed"
                )
                
                foreach ($pattern in $patterns) {
                    $found = Get-ChildItem -Path "$drive\" -Filter "RimWorldWin64_Data" -Recurse -Directory -ErrorAction SilentlyContinue | 
                             Where-Object { $_.FullName -like "*$pattern*" } |
                             Select-Object -ExpandProperty FullName -First 5
                    
                    if ($found) {
                        foreach ($path in $found) {
                            $managedPath = Join-Path $path "Managed"
                            if (Test-Path $managedPath) {
                                $driveSearchPaths += $managedPath
                            }
                        }
                    }
                }
            } catch {
                # Silently continue if drive search fails
            }
        }
    }
    
    # Combine all search paths
    $allPaths = $searchPaths + $driveSearchPaths | Select-Object -Unique
    
    foreach ($path in $allPaths) {
        Write-Host "  Checking: $path" -ForegroundColor DarkGray
        if (Test-RimWorldPath $path) {
            $RimWorldPath = $path
            Write-Host "Found RimWorld at: $RimWorldPath" -ForegroundColor Green
            break
        }
    }
    } # End of if ([string]::IsNullOrEmpty($RimWorldPath))
}

if ([string]::IsNullOrEmpty($RimWorldPath)) {
    Write-Host "ERROR: RimWorld installation not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please do one of the following:" -ForegroundColor Yellow
    Write-Host "1. Install RimWorld via Steam, Epic Games, or GOG"
    Write-Host "2. Copy the required DLLs to a 'Lib' folder in the project root"
    Write-Host "3. Run this script with -RimWorldPath parameter:"
    Write-Host "   .\build.ps1 -RimWorldPath 'C:\Your\RimWorld\Path\RimWorldWin64_Data\Managed'"
    Write-Host ""
    Write-Host "Required DLLs:" -ForegroundColor Yellow
    Write-Host "- Assembly-CSharp.dll"
    Write-Host "- 0Harmony.dll"
    Write-Host "- Unity.TextMeshPro.dll"
    Write-Host "- UnityEngine.CoreModule.dll"
    Write-Host "- UnityEngine.IMGUIModule.dll"
    
    Read-Host "Press Enter to exit"
    exit 1
}

# If we found a RimWorld installation (not local Lib), copy DLLs for future builds
if (-not $RimWorldPath.EndsWith("Lib")) {
    $libPath = ".\Lib"
    if (-not (Test-Path $libPath)) {
        New-Item -ItemType Directory -Path $libPath -Force | Out-Null
    }

    Write-Host "Copying required DLLs to local Lib folder..." -ForegroundColor Yellow
    $dllsToCopy = @(
        "Assembly-CSharp.dll",
        "Unity.TextMeshPro.dll",
        "UnityEngine.CoreModule.dll",
        "UnityEngine.IMGUIModule.dll"
    )

    foreach ($dll in $dllsToCopy) {
        $sourcePath = Join-Path $RimWorldPath $dll
        $destPath = Join-Path $libPath $dll
        if (Test-Path $sourcePath) {
            Copy-Item $sourcePath $destPath -Force
            Write-Host "  Copied $dll" -ForegroundColor Gray
        }
    }
    
    # Download Harmony for local Lib
    Get-HarmonyDll $libPath | Out-Null
}

# Build the project
Write-Host ""
Write-Host "Building DMC Abilities..." -ForegroundColor Yellow
$buildResult = & dotnet build -p:RimWorldPath="$RimWorldPath"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "BUILD SUCCESSFUL!" -ForegroundColor Green
    Write-Host "The compiled DLL has been placed in: ..\..\Assemblies\DMCAbilities.dll" -ForegroundColor Green
    Write-Host ""
    Write-Host "To install the mod:" -ForegroundColor Cyan
    Write-Host "1. Copy the entire DMC_Abilities folder to your RimWorld\Mods\ directory"
    Write-Host "2. Enable the mod in RimWorld mod list"
    Write-Host "3. Add your custom texture files to the Textures folder"
    Write-Host ""
    Write-Host "Texture files needed:" -ForegroundColor Yellow
    Write-Host "- Textures\UI\Abilities\Stinger.png (64x64)"
    Write-Host "- Textures\UI\Abilities\JudgementCut.png (64x64)"
    Write-Host "- Textures\Things\Item\Misc\SkillbookStinger.png (64x64)"
    Write-Host "- Textures\Things\Item\Misc\SkillbookJudgementCut.png (64x64)"
} else {
    Write-Host ""
    Write-Host "BUILD FAILED! Check the errors above." -ForegroundColor Red
}

Write-Host ""
Read-Host "Press Enter to exit"