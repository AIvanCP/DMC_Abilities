# GitHub Upload Instructions

## Option 1: Using GitHub Website (Recommended)

1. **Go to GitHub.com** and sign in to your account
2. **Click "New repository"** (+ icon in top right)
3. **Repository settings:**
   - Name: `rimworld-dmc-abilities`
   - Description: `Devil May Cry inspired abilities mod for RimWorld - Adds Stinger dash and Judgement Cut abilities`
   - Public repository
   - Don't initialize with README (we already have one)

4. **Copy the remote URL** from the created repository page

5. **In PowerShell, run these commands:**

```powershell
# Navigate to the mod folder (already done)
cd "D:\0-tugas-IK-D\projek-gabut\skill_dmc\DMC_Abilities"

# Add GitHub remote (replace YOUR_USERNAME with your GitHub username)
git remote add origin https://github.com/YOUR_USERNAME/rimworld-dmc-abilities.git

# Push to GitHub
git branch -M main
git push -u origin main
```

## Option 2: Using GitHub Desktop

1. **Download GitHub Desktop** from https://desktop.github.com/
2. **Install and sign in** to your GitHub account
3. **Add existing repository:**
   - File → Add Local Repository
   - Choose: `D:\0-tugas-IK-D\projek-gabut\skill_dmc\DMC_Abilities`
4. **Publish repository:**
   - Click "Publish repository"
   - Name: `rimworld-dmc-abilities`
   - Description: `Devil May Cry inspired abilities mod for RimWorld`
   - Keep public
   - Click "Publish"

## What's Already Done ✅

- ✅ Cleaned up all unnecessary files
- ✅ Created concise README.md
- ✅ Initialized Git repository
- ✅ Created .gitignore for proper file exclusions
- ✅ Made initial commit with all clean files
- ✅ Repository is ready to push to GitHub

## Files Included in Upload

The repository contains only the essential files:
- **About/** - Mod metadata
- **Assemblies/** - Compiled mod DLL
- **Defs/** - XML game definitions
- **Languages/** - Text translations
- **Source/** - C# source code & build script
- **Textures/** - Placeholder texture files
- **README.md** - Installation and usage guide
- **.gitignore** - Git exclusion rules

All development artifacts and redundant documentation have been removed!