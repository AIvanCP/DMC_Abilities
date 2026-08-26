# DMC Abilities

A comprehensive RimWorld mod bringing **11 Devil May Cry-inspired abilities** including the legendary **Devil Trigger** and **Sin Devil Trigger** transformations, with advanced mechanics, skill-based scaling, and extensive customization options.

## ⚔️ Combat Abilities

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

### 🔥 **TRANSFORMATION ABILITIES** 🔥

#### **🔴 Devil Trigger** *(Legendary Rare - 0.5% spawn rate)*
**Duration**: 30 seconds | **Cooldown**: 2.5 minutes (reduces with combat)

**Enhanced Combat Power:**
- **🗡️ +1.5x Melee Damage** | **🏹 +1.3x Ranged Damage**
- **🎯 +20% Hit Chance** | **🤸 +25% Dodge Chance**  
- **🏃 +2.0 Move Speed** | **📍 +30% Accuracy (all ranges)**

**Demonic Resilience:**
- **🛡️ 25% Damage Reduction** (all damage types)
- **⚔️ +25% Armor** (Sharp/Blunt) | **🔥 +20% Heat Resistance**
- **😵 +30% Pain Shock Threshold** | **⚡ 50% Stagger Reduction**

**Supernatural Regeneration:**
- **❤️ 2 HP per second active healing**
- **🩹 +3x Injury Healing Factor** | **🧬 8x Natural Healing Rate**
- **💓 Enhanced blood system** (+50% filtration, +30% pumping)

---

#### **🟣 Sin Devil Trigger** *(Mythical Rare - 0.2% spawn rate)*
**Duration**: 60 seconds | **Cooldown**: 5 minutes (reduces with combat)

*Includes ALL Devil Trigger bonuses but significantly enhanced:*

**Ultimate Combat Supremacy:**
- **⚔️ +2.5x Melee Damage** | **🏹 +2.0x Ranged Damage**
- **🎯 +35% Hit Chance** | **🤸 +50% Dodge Chance**
- **🏃 +3.5 Move Speed + 2x Speed Factor** (multiplicative)
- **📍 +60% Short/Medium Accuracy** | **🎯 +50% Long Range**

**Divine Protection:**
- **🛡️ 50% Damage Reduction** (near-immunity)
- **⚔️ +40% Armor** (Sharp/Blunt) | **🔥 +50% Heat Resistance**  
- **😵 +60% Pain Shock Threshold** | **⚡ 80% Stagger Reduction**
- **🧠 Immunity to mental breaks**

**Ultimate Regeneration:**
- **❤️ 10 HP per 0.5 seconds** (ultra-fast healing)
- **🩹 +5x Injury Healing Factor** | **🔄 Heals 3 injuries simultaneously**
- **🦴 Slowly heals permanent injuries** | **💓 Perfect blood system**

**Transcendent Powers:**
- **🌍 Terrain Movement Immunity** (no movement penalties)
- **🚫 Status Immunity**: Stun, Paralyze, Toxic, Temperature extremes
- **🧠 Enhanced capacities**: +30% Consciousness, +40% Movement, +30% Manipulation

### ⏱️ **Dynamic Cooldown System**
Both transformations feature **combat-responsive cooldowns**:
- **-0.1 seconds per damage dealt** | **-0.5 seconds per enemy killed**
- Rewards active combat and skillful play

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

## Half-Demon integration

If the **Half-Demon** mod (`AIvanCP.halfdemon`) is also installed, one extra feature turns
on: **demonic resurgence**.

When something would kill a pawn carrying the `DMC_DemonicBlood` gene, the mod rolls once
(15% by default, slider in mod settings). On a hit the death is cancelled and the demonic
half takes over - the pawn stays standing, still torn apart, and transforms on the spot
(Sin Devil Trigger if they have it, otherwise Devil Trigger).

**How the death is cancelled matters.** Not by healing. `Pawn_HealthTracker.ShouldBeDead()`
opens with:

```csharp
if (hediffSet.HasPreventsDeath) return false;
```

That is the same lever vanilla's Deathless gene pulls, so a hediff with `preventsDeath true`
keeps the pawn alive without touching a single wound. They get up ruined, which is the
point.

The catch, and it is a real one: the resurgence hediff lasts 30 seconds. When it expires the
wounds are all still there. Untreated, the pawn dies anyway. This buys a second chance, not
immortality. A 2-day exhaustion hediff stops it firing twice in one fight, and a destroyed
brain stops it entirely - the same line Deathless draws.

The two mods do **not** hard-depend on each other. The gene is resolved by name with
`GetNamedSilentFail`, so DMC Abilities works normally with Half-Demon absent, and Half-Demon
works normally with DMC Abilities absent (you just get the body and no transformations).

## Installation

1. Download the mod
2. Extract to `RimWorld/Mods/DMC_Abilities/`
3. Enable in RimWorld's mod menu
4. Restart the game

## 📖 Usage Guide

### 📚 Learning Abilities

- **Acquire Skillbooks**: Available through traders, quests, raids, or dev mode
- **Rarity Tiers**:
  - **Common** (4-5% spawn): Stinger, Gun Stinger, Drive, Rain Bullet
  - **Uncommon** (3-4% spawn): Judgement Cut, Rapid Slash, Void Slash, Red Hot Night
  - **Rare** (1% spawn): Heavy Rain (4000-6000 silver)
  - **Legendary** (0.5% spawn): **Devil Trigger** (8000-12000 silver)
  - **Mythical** (0.2% spawn): **Sin Devil Trigger** (15000-25000 silver)
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

**Ultimate Abilities**:
- **Heavy Rain**: Spectral sword rain (15+ swords) with legendary damage scaling
- **Devil Trigger**: 30-second transformation with enhanced combat and regeneration
- **Sin Devil Trigger**: 60-second ultimate form with near-godlike power

### 🎮 **Transformation Strategy Guide**

**Devil Trigger Best Uses:**
- Extended combat encounters requiring sustained power
- When injured and need rapid regeneration
- Fighting multiple tough enemies or mechanoids  
- Breaking through heavily defended positions

**Sin Devil Trigger Best Uses:**
- Boss fights against legendary threats (thrumbos, etc.)
- Last-resort situations when colony is under siege
- Clearing entire enemy bases or infestations
- When facing overwhelming odds (large raids, etc.)

**Cooldown Management Tips:**
- Stay aggressive to reduce cooldowns faster
- Each kill gives 0.5 seconds reduction - prioritize killing blows
- Each damage instance gives 0.1 seconds reduction
- Plan transformation usage around major encounters
- Don't waste transformations on weak enemies

### ⚙️ Mod Settings

**Core Options**:
- **Master Toggle**: Enable/disable entire mod
- **Friendly Fire Protection**: Prevent damage to colonists and allies (all 11 abilities)
- **Individual Ability Toggles**: Enable/disable each of the 11 abilities separately

**Transformation Settings**:
- **Devil Trigger Damage Multiplier**: 1.0x - 3.0x (default: 1.5x)
- **Sin Devil Trigger Damage Multiplier**: 1.0x - 4.0x (default: 2.0x)
- **Transformation Trade Rates**: Adjust skillbook availability in traders

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

## 🤝 **Universal Mod Compatibility**

### 🎯 **Advanced Compatibility Features**:
- **🔫 Universal Weapon Support**: Detects and works with ANY modded weapon through intelligent analysis
- **🤖 Modded Race Integration**: Full support for alien races, androids, and custom humanlike species  
- **🏛️ Faction System Compatibility**: Advanced relationship detection for modded factions and diplomacy
- **🧠 Smart Targeting**: Enhanced friendly fire protection that understands complex mod relationships
- **⚡ Performance Optimized**: Graceful error handling prevents crashes with broken mods

### ✅ **Extensively Tested With**:
- **Weapon Mods**: Combat Extended, Vanilla Weapons Expanded (all variants), Medieval Overhaul, Rimmu-Nation, etc.
- **Combat Overhauls**: Combat Extended (full compatibility), Yayo's Combat 3, Simple Sidearms
- **Race Mods**: Humanoid Alien Races framework, Android Tiers, Ratkin, Kurin, Revia, etc.
- **Faction Mods**: Vanilla Factions Expanded (all), Empire, Glitterworld Medicine, etc.  
- **Ability Frameworks**: Vanilla Psycasts Expanded, Alpha Animals psycasts, RimWorld of Magic
- **Performance Mods**: Rocketman, Performance Optimizer, RuntimeGC, Camera+, etc.

### 🔧 **Smart Detection Systems**:
- **Enhanced Shotgun Recognition**: 50+ weapon patterns including modded variants
- **Intelligent Damage Scaling**: Adapts to modded weapon stats (market value, mass, tech level)
- **Advanced Friendly Fire**: Protects relationships, bonded animals, and faction allies
- **Conditional Patching**: Only applies necessary patches, prevents conflicts

### ⚠️ **Recommended Load Order**:
1. **Core Game**: RimWorld + Official DLCs
2. **Framework Mods**: Harmony, HugsLib, Alien Race Framework  
3. **Major Overhauls**: Combat Extended, Psychology, etc.
4. **Weapon & Combat Mods**: VWE series, combat mods
5. **🎯 DMC Abilities** (this mod - load after combat frameworks)
6. **Content Mods**: Faction mods, additional content
7. **UI/QoL Mods**: Interface enhancements, quality of life

### 💾 **Save Compatibility**:
- ✅ **Safe to add** to existing saves (abilities become available immediately)
- ✅ **Safe to remove** (clean removal, no broken references)
- ✅ **Mid-playthrough friendly** with full backwards compatibility

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

### Version 3.0.0 (Current) - **Devil Trigger Update**
- 🔥 **NEW: Devil Trigger & Sin Devil Trigger transformations**
- ✅ **DMC 5 lore-accurate features**: Health regeneration, damage reduction, status immunities
- ✅ **Dynamic cooldown system**: Reduces with combat performance (damage/kills)
- ✅ **Enhanced mod compatibility**: Works with 40+ known weapon and combat mods
- ✅ **Legendary rarity system**: Mythical transformation skillbooks (0.2-0.5% spawn rates)
- ✅ **Ultimate regeneration**: Active healing and injury recovery systems
- ✅ **Terrain immunity**: SDT ignores all movement penalties
- ✅ **Visual enhancements**: Color-coded transformation auras and effects
- ✅ **Settings expansion**: Individual transformation multipliers and trade rates

### Version 2.0.0
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
