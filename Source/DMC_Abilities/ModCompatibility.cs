using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace DMCAbilities
{
    [StaticConstructorOnStartup]
    public static class ModCompatibility
    {
        private static readonly List<string> KnownConflictingMods = new List<string>
        {
            // Add any known conflicting mod package IDs here
            // Example: "author.modname.conflicting"
        };

        private static readonly List<string> RecommendedCompanionMods = new List<string>
        {
            "CETeam.CombatExtended", // Combat Extended (for weapon variety)
            "VanillaExpanded.VanillaWeaponsExpanded", // More weapons to use abilities with
            "VanillaExpanded.VanillaPsycastsExpanded" // Psycasts compatibility
        };

        static ModCompatibility()
        {
            CheckModCompatibility();
        }

        private static void CheckModCompatibility()
        {
            var loadedMods = ModsConfig.ActiveModsInLoadOrder.Select(m => m.PackageId).ToList();
            
            // Check for conflicting mods
            foreach (string conflictingMod in KnownConflictingMods)
            {
                if (loadedMods.Contains(conflictingMod))
                {
                    Log.Warning($"[DMC Abilities] Potentially conflicting mod detected: {conflictingMod}. " +
                               "Some features may not work as expected.");
                }
            }

            // Log companion mods if found
            foreach (string companionMod in RecommendedCompanionMods)
            {
                if (loadedMods.Contains(companionMod))
                {
                    Log.Message($"[DMC Abilities] Compatible companion mod detected: {companionMod}");
                }
            }

            // Check load order
            CheckLoadOrder(loadedMods);
            
            Log.Message("[DMC Abilities] Mod compatibility check completed.");
        }

        private static void CheckLoadOrder(List<string> loadedMods)
        {
            string ourModId = "dmcabilities.rimworld.devilmaycry";
            int ourIndex = loadedMods.FindIndex(m => m.ToLower().Contains("dmcabilities") || m == ourModId);
            
            if (ourIndex == -1)
            {
                Log.Warning("[DMC Abilities] Could not determine mod load order position.");
                return;
            }

            // Check if we're loaded after core game modules
            var coreModIndex = loadedMods.FindIndex(m => m.Contains("Ludeon.RimWorld"));
            if (coreModIndex >= 0 && ourIndex <= coreModIndex)
            {
                Log.Warning("[DMC Abilities] Mod is loaded before RimWorld core. This may cause issues. " +
                           "Consider moving DMC Abilities lower in the load order.");
            }

            // Check if we're loaded after ability frameworks
            var abilityFrameworks = new[] { "royalty", "ideology", "biotech", "psycast", "ability" };
            foreach (string framework in abilityFrameworks)
            {
                var frameworkIndex = loadedMods.FindIndex(m => m.ToLower().Contains(framework));
                if (frameworkIndex >= 0 && ourIndex <= frameworkIndex)
                {
                    Log.Message($"[DMC Abilities] Loading after {framework} framework - good load order.");
                }
            }
        }

        public static bool IsModCompatible(string packageId)
        {
            return !KnownConflictingMods.Contains(packageId);
        }

        public static bool HasCompatibleWeaponMods()
        {
            var loadedMods = ModsConfig.ActiveModsInLoadOrder.Select(m => m.PackageId.ToLower()).ToList();
            
            // Check for common weapon mods
            var weaponModKeywords = new[] { "weapon", "melee", "sword", "blade", "combat" };
            return loadedMods.Any(mod => weaponModKeywords.Any(keyword => mod.Contains(keyword)));
        }
    }
}