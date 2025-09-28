using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using System;

namespace DMCAbilities
{
    [StaticConstructorOnStartup]
    public static class ModCompatibility
    {
        // Expanded compatibility system for better mod support
        private static readonly Dictionary<string, CompatibilityInfo> KnownMods = new Dictionary<string, CompatibilityInfo>
        {
            // Combat and Weapon Mods (High Compatibility)
            {"CETeam.CombatExtended", new CompatibilityInfo("Combat Extended", CompatibilityLevel.HighCompatibility, "Enhanced combat mechanics work well with DMC abilities")},
            {"VanillaExpanded.VanillaWeaponsExpanded", new CompatibilityInfo("Vanilla Weapons Expanded", CompatibilityLevel.HighCompatibility, "More weapons to use with DMC abilities")},
            {"VanillaExpanded.VanillaWeaponsExpandedMelee", new CompatibilityInfo("VWE - Melee", CompatibilityLevel.HighCompatibility, "Additional melee weapons for DMC abilities")},
            {"VanillaExpanded.VanillaWeaponsExpandedRanged", new CompatibilityInfo("VWE - Ranged", CompatibilityLevel.HighCompatibility, "Additional ranged weapons for Gun Stinger")},
            
            // Ability and Psycast Mods (Medium Compatibility)
            {"VanillaExpanded.VanillaPsycastsExpanded", new CompatibilityInfo("Vanilla Psycasts Expanded", CompatibilityLevel.MediumCompatibility, "Both ability systems coexist well")},
            {"VanillaExpanded.VanillaAbilitiesExpanded", new CompatibilityInfo("Vanilla Abilities Expanded", CompatibilityLevel.MediumCompatibility, "Compatible ability framework")},
            {"sarg.alphaanimals", new CompatibilityInfo("Alpha Animals", CompatibilityLevel.MediumCompatibility, "Works with modded creatures")},
            
            // UI and Enhancement Mods (High Compatibility)
            {"Fluffy.ModManager", new CompatibilityInfo("Mod Manager", CompatibilityLevel.HighCompatibility, "Better mod management")},
            {"UnlimitedHugs.HugsLib", new CompatibilityInfo("HugsLib", CompatibilityLevel.HighCompatibility, "Mod framework compatibility")},
            {"Dubwise.DubsPerformanceAnalyzer", new CompatibilityInfo("Dubs Performance Analyzer", CompatibilityLevel.HighCompatibility, "Performance monitoring compatibility")},
            
            // Race and Faction Mods (Medium Compatibility) 
            {"OskarPotocki.VanillaFactionsExpanded.Core", new CompatibilityInfo("Vanilla Factions Expanded", CompatibilityLevel.MediumCompatibility, "Works with modded factions")},
            {"erdelf.HumanoidAlienRaces", new CompatibilityInfo("Humanoid Alien Races", CompatibilityLevel.MediumCompatibility, "Compatible with alien races")},
            
            // Known Issues (Low Compatibility - need special handling)
            {"Ludeon.RimWorld.Royalty", new CompatibilityInfo("Royalty DLC", CompatibilityLevel.RequiredDependency, "Core ability framework")},
            {"Ludeon.RimWorld.Ideology", new CompatibilityInfo("Ideology DLC", CompatibilityLevel.HighCompatibility, "Compatible with ideology systems")},
            {"Ludeon.RimWorld.Biotech", new CompatibilityInfo("Biotech DLC", CompatibilityLevel.HighCompatibility, "Compatible with gene systems")},
            {"Ludeon.RimWorld.Anomaly", new CompatibilityInfo("Anomaly DLC", CompatibilityLevel.HighCompatibility, "Compatible with anomaly systems")}
        };

        public enum CompatibilityLevel
        {
            RequiredDependency,    // Must have for DMC to work properly
            HighCompatibility,     // Works perfectly together
            MediumCompatibility,   // Works well with minor considerations
            LowCompatibility,      // May have issues, special handling needed
            Incompatible          // Known conflicts
        }

        public struct CompatibilityInfo
        {
            public string DisplayName;
            public CompatibilityLevel Level;
            public string Description;

            public CompatibilityInfo(string displayName, CompatibilityLevel level, string description)
            {
                DisplayName = displayName;
                Level = level;
                Description = description;
            }
        }

        // Cached mod detection results
        private static Dictionary<string, bool> _loadedModsCache;

        static ModCompatibility()
        {
            try
            {
                CheckModCompatibility();
            }
            catch (Exception ex)
            {
                Log.Error($"[DMC Abilities] Error during mod compatibility check: {ex}");
            }
        }

        private static void CheckModCompatibility()
        {
            var loadedMods = ModsConfig.ActiveModsInLoadOrder.Select(m => m.PackageId.ToLower()).ToList();
            _loadedModsCache = new Dictionary<string, bool>();
            
            int compatibleMods = 0;
            int incompatibleMods = 0;

            // Check all known mods
            foreach (var mod in KnownMods)
            {
                string packageId = mod.Key.ToLower();
                bool isLoaded = loadedMods.Contains(packageId);
                _loadedModsCache[mod.Key] = isLoaded;

                if (isLoaded)
                {
                    var info = mod.Value;
                    switch (info.Level)
                    {
                        case CompatibilityLevel.RequiredDependency:
                            Log.Message($"[DMC Abilities] ✓ Required dependency found: {info.DisplayName}");
                            break;
                        case CompatibilityLevel.HighCompatibility:
                            Log.Message($"[DMC Abilities] ✓ High compatibility mod detected: {info.DisplayName} - {info.Description}");
                            compatibleMods++;
                            break;
                        case CompatibilityLevel.MediumCompatibility:
                            Log.Message($"[DMC Abilities] ◐ Medium compatibility mod detected: {info.DisplayName} - {info.Description}");
                            compatibleMods++;
                            break;
                        case CompatibilityLevel.LowCompatibility:
                            Log.Warning($"[DMC Abilities] ⚠ Low compatibility mod detected: {info.DisplayName} - {info.Description}");
                            break;
                        case CompatibilityLevel.Incompatible:
                            Log.Error($"[DMC Abilities] ✗ Incompatible mod detected: {info.DisplayName} - May cause issues!");
                            incompatibleMods++;
                            break;
                    }
                }
            }

            // Check load order and unknown mods
            CheckLoadOrder(loadedMods);
            CheckUnknownMods(loadedMods);
            
            Log.Message($"[DMC Abilities] Compatibility check completed. Compatible: {compatibleMods}, Issues: {incompatibleMods}");
        }

        private static void CheckLoadOrder(List<string> loadedMods)
        {
            string ourModId = "dmcabilities.rimworld.devilmaycry".ToLower();
            int ourIndex = loadedMods.FindIndex(m => m.Contains("dmcabilities") || m == ourModId);
            
            if (ourIndex == -1)
            {
                Log.Warning("[DMC Abilities] Could not determine mod load order position.");
                return;
            }

            // Check if we're loaded after core game modules
            var coreModIndex = loadedMods.FindIndex(m => m.Contains("ludeon.rimworld"));
            if (coreModIndex >= 0 && ourIndex <= coreModIndex)
            {
                Log.Warning("[DMC Abilities] ⚠ Loading before RimWorld core. Consider moving DMC Abilities lower in load order.");
            }

            // Check if we're loaded after important frameworks
            CheckFrameworkOrder(loadedMods, ourIndex, "harmony", "Harmony framework");
            CheckFrameworkOrder(loadedMods, ourIndex, "royalty", "Royalty DLC");
        }

        private static void CheckFrameworkOrder(List<string> loadedMods, int ourIndex, string framework, string displayName)
        {
            var frameworkIndex = loadedMods.FindIndex(m => m.Contains(framework.ToLower()));
            if (frameworkIndex >= 0)
            {
                if (ourIndex > frameworkIndex)
                {
                    Log.Message($"[DMC Abilities] ✓ Loading after {displayName} - correct load order.");
                }
                else
                {
                    Log.Warning($"[DMC Abilities] ⚠ Loading before {displayName} - may cause issues.");
                }
            }
        }

        private static void CheckUnknownMods(List<string> loadedMods)
        {
            // Look for potentially problematic mod patterns
            var weaponMods = loadedMods.Count(m => m.Contains("weapon") && !KnownMods.ContainsKey(m));
            var abilityMods = loadedMods.Count(m => (m.Contains("ability") || m.Contains("psycast")) && !KnownMods.ContainsKey(m));
            
            if (weaponMods > 0)
            {
                Log.Message($"[DMC Abilities] Detected {weaponMods} unknown weapon mod(s) - should be compatible.");
            }
            if (abilityMods > 0)
            {
                Log.Message($"[DMC Abilities] Detected {abilityMods} unknown ability mod(s) - monitor for conflicts.");
            }
        }

        public static bool IsModCompatible(string packageId)
        {
            if (KnownMods.ContainsKey(packageId))
            {
                return KnownMods[packageId].Level != CompatibilityLevel.Incompatible;
            }
            return true; // Unknown mods are assumed compatible
        }

        public static bool HasCompatibleWeaponMods()
        {
            if (_loadedModsCache == null) return false;
            
            // Check for known compatible weapon mods
            var compatibleWeaponMods = KnownMods.Where(kvp => 
                kvp.Value.Level >= CompatibilityLevel.MediumCompatibility && 
                (kvp.Key.ToLower().Contains("weapon") || kvp.Key.ToLower().Contains("melee")));
            
            return compatibleWeaponMods.Any(mod => _loadedModsCache.ContainsKey(mod.Key) && _loadedModsCache[mod.Key]);
        }

        public static bool IsModLoaded(string packageId)
        {
            return _loadedModsCache?.ContainsKey(packageId) == true && _loadedModsCache[packageId];
        }

        public static CompatibilityLevel GetModCompatibilityLevel(string packageId)
        {
            return KnownMods.ContainsKey(packageId) ? KnownMods[packageId].Level : CompatibilityLevel.MediumCompatibility;
        }

        public static List<string> GetLoadedCompatibleMods()
        {
            if (_loadedModsCache == null) return new List<string>();
            
            return KnownMods.Where(kvp => 
                _loadedModsCache.ContainsKey(kvp.Key) && 
                _loadedModsCache[kvp.Key] && 
                kvp.Value.Level >= CompatibilityLevel.MediumCompatibility)
                .Select(kvp => kvp.Value.DisplayName)
                .ToList();
        }
    }
}