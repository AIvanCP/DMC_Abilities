# DMC Abilities

A RimWorld mod that adds two Devil May Cry-inspired melee abilities.

## Features

### Abilities
- **Stinger** - Lightning-fast dash attack with 1.2x damage multiplier
- **Judgement Cut** - Ranged dimensional slash creating 1-3 AoE cuts

### Compatibility
- ✅ **Full modded weapon support** - Works with ANY melee weapon
- ✅ **RimWorld 1.6 compatible**
- ✅ **Harmony patches** for seamless mod compatibility
- ✅ **Configurable settings** panel

## Installation

1. Download the mod
2. Extract to `RimWorld/Mods/DMC_Abilities/`
3. Enable in RimWorld's mod menu
4. Restart the game

## Usage

### Learning Abilities
- Craft or spawn skillbooks: `DMC_StingerSkillbook` / `DMC_JudgementCutSkillbook`
- Right-click skillbook → "Learn ability"
- Abilities appear in character's abilities tab

### Using Abilities
- **Stinger**: Select ability → Click target → Instant dash + attack
- **Judgement Cut**: Select ability → Click area → Warmup → AoE slashes

## Requirements

- RimWorld 1.6+
- Harmony (included)

## Building from Source

**Prerequisites:** Copy required RimWorld DLLs to `Source/DMC_Abilities/Lib/`:
- `Assembly-CSharp.dll`
- `0Harmony.dll` 
- `Unity.TextMeshPro.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.IMGUIModule.dll`

**Build:**
```powershell
cd Source/DMC_Abilities
./build.ps1
```

The DLLs can be found in your RimWorld installation and mod folders.

## License

This mod is free to use and modify.