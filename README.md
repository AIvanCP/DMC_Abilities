# DMC Abilities

A comprehensive RimWorld mod that adds eight Devil May Cry-inspired abilities with advanced mechanics.

## Features

### Abilities
- **Stinger** - Lightning-fast dash attack with 1.2x damage multiplier  
- **Gun Stinger** - Ranged piercing shot with area blast on impact
- **Judgement Cut** - Ranged dimensional slash creating 1-3 AoE cuts with spectral wounds
- **Void Slash** - Multi-hit combo with stagger effects and void debuff
- **Rapid Slash** - Forward dash with spectral summoned swords that explode
- **Rain Bullet** - Teleport-based bullet storm with path damage
- **Drive** - Charging slash attack with stagger immunity
- **Heavy Rain** - Ultimate AoE ability (extremely rare skillbook)

### Advanced Features
- ✅ **Uncapped skill scaling** - Abilities scale beyond level 20 for endgame characters
- ✅ **Smart targeting system** - Automatically prioritizes enemies (70% chance)
- ✅ **Obstacle bypassing** - Abilities can work around walls and barriers
- ✅ **Friendly fire toggle** - Optional setting to avoid hitting allies (default: OFF)
- ✅ **Universal targeting** - Works on pawns, animals, mechs, and turrets
- ✅ **Spectral wound system** - Unique damage-over-time effects
- ✅ **Stagger mechanics** - Crowd control with immunity frames

### Compatibility
- ✅ **Full modded weapon support** - Works with ANY melee weapon
- ✅ **RimWorld 1.6+ compatible**
- ✅ **Harmony patches** for seamless mod compatibility
- ✅ **Comprehensive settings** panel with sliders and toggles

## Installation

1. Download the mod
2. Extract to `RimWorld/Mods/DMC_Abilities/`
3. Enable in RimWorld's mod menu
4. Restart the game

## Usage

### Learning Abilities
- **Obtain skillbooks** through traders, quests, or dev mode
- **Rarity levels**: Common abilities (15% spawn) → Heavy Rain (1% spawn, 4000-6000 silver)
- Right-click skillbook → "Learn ability"
- Abilities appear in character's abilities tab

### Using Abilities
- **Stinger**: Dash to target with precise positioning and knockback
- **Gun Stinger**: Ranged shot that creates explosion on impact
- **Judgement Cut**: AoE slashes that inflict spectral wounds
- **Void Slash**: Multi-hit combo with stagger and void damage over time
- **Rapid Slash**: Dash forward while summoning exploding spectral swords
- **Rain Bullet**: Teleport and unleash bullet storm with path damage
- **Drive**: Charging attack with stagger immunity during windup
- **Heavy Rain**: Devastating AoE with massive damage potential

### Mod Settings
- **Friendly Fire Toggle**: Prevent abilities from hitting colonists/allies
- **Ability Cooldowns**: Customize cooldown periods for each ability
- **Damage Multipliers**: Adjust damage scaling and balance

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
