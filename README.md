# DMC Abilities

A comprehensive RimWorld mod that brings **nine Devil May Cry-inspired abilities** with advanced mechanics, skill-based scaling, and extensive customization options.

## ⚔️ Abilities

### Melee Abilities
- **🗡️ Stinger** - Lightning-fast dash attack with precise positioning and knockback effects
- **⚡ Rapid Slash** - Multi-hit forward dash with spectral sword summons that create explosions  
- **🌀 Void Slash** - Devastating cone attack inflicting void debuffs and guaranteed stagger
- **💫 Judgement Cut** - Dimensional slash creating 1-3 AoE cuts with spectral wound DoT
- **🚀 Drive** - Charging slash projectile with stagger immunity and armor penetration

### Ranged Abilities  
- **🔫 Gun Stinger** - Shotgun dash with point-blank blast and cone area damage
- **🌧️ Rain Bullet** - Teleporting bullet storm dealing damage along path and at destination
- **🔥 Red Hot Night** - Falling fire orbs with explosive damage and burn debuffs
- **⚔️ Heavy Rain** - Ultimate spectral sword rain (legendary ability)

## ✨ Advanced Features

### 🎯 Combat System
- ✅ **Skill-Based Scaling** - Abilities scale with Melee/Shooting skills (uncapped, beyond level 20)
- ✅ **Weapon Integration** - Damage scales with equipped weapon stats for authentic combat
- ✅ **Smart Targeting** - 70% chance to prioritize enemies, 30% random for tactical variety
- ✅ **Obstacle Bypassing** - Dash through walls and barriers with proper positioning
- ✅ **Universal Damage** - Affects pawns, animals, mechs, and turrets consistently

### 🛡️ Safety & Control
- ✅ **Comprehensive Friendly Fire Protection** - Toggle to prevent ally damage (all 9 abilities)
- ✅ **Faction Awareness** - Respects relationships and colonist status automatically
- ✅ **Null Safety** - Robust error handling prevents crashes and position bugs
- ✅ **Performance Optimized** - Efficient damage calculations and effect rendering

### ⚙️ Customization
- ✅ **Individual Damage Multipliers** - Separate sliders for each ability (0.5x - 3.0x)
- ✅ **Ability Toggles** - Enable/disable each ability independently
- ✅ **Trader Frequency** - Adjust skillbook spawn rates in trader inventories
- ✅ **Performance Settings** - Configure maximum projectile counts and effect intensity

### 🔧 Technical Excellence
- ✅ **RimWorld 1.6+ Compatible** - Latest game version support
- ✅ **Full Mod Compatibility** - Works seamlessly with weapon and combat mods
- ✅ **Harmony Integration** - Non-intrusive patches for maximum compatibility
- ✅ **Clean Logging** - Minimal debug output to preserve log readability

## Installation

1. Download the mod
2. Extract to `RimWorld/Mods/DMC_Abilities/`
3. Enable in RimWorld's mod menu
4. Restart the game

## 📖 Usage Guide

### 📚 Learning Abilities

- **Acquire Skillbooks**: Available through traders, quests, raids, or dev mode
- **Rarity Tiers**: 
  - **Common** (4% spawn): Stinger, Gun Stinger, Judgement Cut, Rapid Slash 
  - **Uncommon** (2% spawn): Void Slash, Rain Bullet, Drive, Red Hot Night
  - **Legendary** (1% spawn): Heavy Rain (4000-6000 silver value)
- **Usage**: Right-click skillbook → "Learn ability" → Appears in character's abilities tab

### ⚔️ Combat Guide

**Melee Abilities** (Scale with Melee skill):
- **Stinger**: Lightning dash + strike, stops adjacent to target, applies knockback
- **Rapid Slash**: Multi-hit dash with 15% chance to summon explosive spectral swords  
- **Void Slash**: 75° cone attack with void debuff, guaranteed stagger effects
- **Judgement Cut**: Ranged dimensional cuts (1-3 slashes) with spectral wound DoT
- **Drive**: Piercing projectile with stagger immunity, ignores 15% armor

**Ranged Abilities** (Scale with Shooting skill):
- **Gun Stinger**: Shotgun dash → point-blank blast + 90° cone damage
- **Rain Bullet**: Teleport + bullet storm, damages path and destination area
- **Red Hot Night**: Raining fire orbs with explosive damage + burn debuffs (13s duration)

**Ultimate Ability**:
- **Heavy Rain**: Spectral sword rain (15+ swords) with legendary damage scaling

### ⚙️ Mod Settings

**Core Options**:
- **Master Toggle**: Enable/disable entire mod
- **Friendly Fire Protection**: Prevent damage to colonists and allies (all abilities)
- **Individual Ability Toggles**: Enable/disable each of the 9 abilities separately

**Damage Customization**:
- **Individual Damage Multipliers**: Separate sliders for each ability (0.5x - 3.0x range)
- **Sword Damage Bonus**: Extra damage when using sword-type weapons (0-50%)
- **Performance Tuning**: Max Red Hot Night orbs (5-50 range)

**Economic Settings**:
- **Trader Spawn Rates**: Adjust how often skillbooks appear in trader inventories
- **Skillbook Values**: All abilities maintain balanced pricing (400-6000 silver)

## 🔧 Requirements

- **RimWorld 1.6+** (Latest version recommended)
- **Harmony** (Auto-downloaded, no manual installation needed)

## 🤝 Compatibility

### ✅ **Fully Compatible**:
- **All weapon mods** (CE, VWE, Medieval, etc.)
- **Combat overhauls** (Combat Extended, Yayo's Combat 3, etc.)
- **Faction mods** (friendly fire protection respects all faction relationships)
- **Performance mods** (Rocketman, Performance Optimizer, etc.)
- **Save games** (safe to add/remove mid-playthrough)

### ⚠️ **Load Order Recommendations**:
1. RimWorld Core + DLCs
2. Harmony  
3. Combat/Weapon framework mods
4. **DMC Abilities** (this mod)
5. Other content mods

## 🚀 Performance Notes

- **Optimized Effects**: Smart rendering prevents FPS drops during ability usage
- **Memory Efficient**: Proper cleanup prevents memory leaks in long-term colonies  
- **Scalable**: Handles large raids and multiple simultaneous ability usage
- **Configurable**: Adjust effect intensity and projectile counts via settings

## 🛠️ Building from Source

**Prerequisites:** Copy required RimWorld DLLs to `Source/DMC_Abilities/Lib/`:

- `Assembly-CSharp.dll`
- `0Harmony.dll`
- `Unity.TextMeshPro.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.IMGUIModule.dll`

**Build Commands:**

```powershell
cd Source/DMC_Abilities
./build.ps1
```

Or use .NET CLI:

```powershell  
dotnet build
```

The compiled DLL will be output to `Assemblies/DMCAbilities.dll`

## 📈 Version History

### Version 2.0.0 (Current)
- ✅ **Added Red Hot Night** - Fire orb rain ability with burn debuffs
- ✅ **Complete friendly fire system** - Protection across all 9 abilities
- ✅ **Individual damage multipliers** - Separate sliders for each ability
- ✅ **Skill system overhaul** - Proper Melee/Shooting skill scaling
- ✅ **Projectile fixes** - Resolved position bugs and collection errors
- ✅ **Performance optimization** - Clean logging and null safety
- ✅ **Enhanced settings** - Comprehensive customization options

### Version 1.0.0
- Initial release with 8 core abilities
- Basic weapon integration and damage scaling
- Trader system and skillbook acquisition

## 📄 License

This mod is released under **MIT License** - free to use, modify, and distribute.

## 🐛 Bug Reports & Support

Please report issues with:
- **RimWorld version** and **mod load order**
- **Detailed steps to reproduce** the problem
- **Log files** (Player.log) if experiencing crashes

For optimal support, test issues with **minimal mod lists** to identify conflicts.
