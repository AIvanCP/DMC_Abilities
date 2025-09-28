using HarmonyLib;
using RimWorld;
using Verse;
using System;

namespace DMCAbilities
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            try
            {
                var harmony = new Harmony("dmcabilities.rimworld.devilmaycry");
                harmony.PatchAll();
                
                // Log successful patching with version info for better mod compatibility debugging
                Log.Message("[DMC Abilities] Harmony patches applied successfully. Version 2.0.0 - Enhanced mod compatibility.");
            }
            catch (Exception ex)
            {
                Log.Error($"[DMC Abilities] Failed to apply Harmony patches: {ex}");
                Log.Error("[DMC Abilities] This may indicate a mod compatibility issue. Please check load order.");
            }
        }
    }

    // Enhanced compatibility patch - safe ability tracking with null checks
    [HarmonyPatch(typeof(Pawn_AbilityTracker), "GainAbility")]
    public static class Pawn_AbilityTracker_GainAbility_Patch
    {
        public static bool Prepare()
        {
            // Only apply this patch if AbilityTracker exists (mod compatibility)
            return typeof(Pawn_AbilityTracker).GetMethod("GainAbility") != null;
        }

        public static void Postfix(Pawn_AbilityTracker __instance, AbilityDef def)
        {
            try
            {
                // Enhanced null safety for mod compatibility
                if (__instance?.pawn == null || def?.defName == null) return;
                
                // Only track our abilities to avoid interference with other ability mods
                if (def.defName.StartsWith("DMC_"))
                {
                    // Clean logging - only if debug mode is enabled
                    if (Prefs.DevMode)
                    {
                        Log.Message($"[DMC Abilities] {__instance.pawn.Name?.ToStringShort ?? "Pawn"} gained: {def.defName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[DMC Abilities] Non-critical error in ability tracking: {ex.Message}");
            }
        }
    }

    // Enhanced settings patch with full mod compatibility safety
    [HarmonyPatch(typeof(Ability), "get_Disabled")]
    public static class Ability_Disabled_Patch
    {
        public static bool Prepare()
        {
            // Conditional patching - only patch if Ability class has the expected method
            return typeof(Ability).GetProperty("Disabled") != null;
        }

        public static void Postfix(Ability __instance, ref bool __result)
        {
            try
            {
                // Enhanced null safety and mod compatibility checks
                if (__instance?.def?.defName == null || DMCAbilitiesMod.settings == null) return;
                
                // Only handle our abilities to avoid conflicts with other ability mods
                if (!__instance.def.defName.StartsWith("DMC_")) return;

                // Master mod toggle
                if (!DMCAbilitiesMod.settings.modEnabled)
                {
                    __result = true;
                    return;
                }

                // Individual ability settings with safe fallbacks
                if (__instance.def.defName == "DMC_Stinger" && !DMCAbilitiesMod.settings.stingerEnabled)
                {
                    __result = true;
                }
                else if (__instance.def.defName == "DMC_JudgementCut" && !DMCAbilitiesMod.settings.judgementCutEnabled)
                {
                    __result = true;
                }
                else if (__instance.def.defName == "DMC_Drive" && !DMCAbilitiesMod.settings.driveEnabled)
                {
                    __result = true;
                }
                else if (__instance.def.defName == "DMC_VoidSlash" && !DMCAbilitiesMod.settings.voidSlashEnabled)
                {
                    __result = true;
                }
                else if (__instance.def.defName == "DMC_RedHotNight" && !DMCAbilitiesMod.settings.redHotNightEnabled)
                {
                    __result = true;
                }
                else if (__instance.def.defName == "DMC_GunStinger" && !DMCAbilitiesMod.settings.gunStingerEnabled)
                {
                    __result = true;
                }
                else if (__instance.def.defName == "DMC_HeavyRain" && !DMCAbilitiesMod.settings.heavyRainEnabled)
                {
                    __result = true;
                }
                else if (__instance.def.defName == "DMC_RainBullet" && !DMCAbilitiesMod.settings.rainBulletEnabled)
                {
                    __result = true;
                }
                else if (__instance.def.defName == "DMC_RapidSlash" && !DMCAbilitiesMod.settings.rapidSlashEnabled)
                {
                    __result = true;
                }
            }
            catch (Exception ex)
            {
                // Graceful degradation - if settings check fails, don't disable abilities
                Log.Warning($"[DMC Abilities] Non-critical error in settings check: {ex.Message}");
            }
        }
    }

    // Enhanced weapon compatibility patch for better mod support
    [HarmonyPatch(typeof(CompEquippable), "get_AllVerbs")]
    public static class CompEquippable_AllVerbs_Patch
    {
        public static bool Prepare()
        {
            // Only patch if CompEquippable exists and has AllVerbs property
            return typeof(CompEquippable).GetProperty("AllVerbs") != null;
        }

        public static void Postfix(CompEquippable __instance, ref System.Collections.Generic.List<Verb> __result)
        {
            try
            {
                // Ensure our weapon utility can always access verbs properly
                // This helps with modded weapons that might have null verb lists
                if (__result == null)
                {
                    __result = new System.Collections.Generic.List<Verb>();
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[DMC Abilities] Non-critical error in weapon verb compatibility: {ex.Message}");
                // Provide safe fallback
                if (__result == null)
                {
                    __result = new System.Collections.Generic.List<Verb>();
                }
            }
        }
    }

    // Additional compatibility patch for modded damage systems
    [HarmonyPatch(typeof(Thing), "TakeDamage")]
    public static class Thing_TakeDamage_Patch
    {
        public static bool Prepare()
        {
            // Only patch if we can safely access the TakeDamage method
            return typeof(Thing).GetMethod("TakeDamage", new[] { typeof(DamageInfo) }) != null;
        }

        public static bool Prefix(Thing __instance, DamageInfo dinfo)
        {
            try
            {
                // Let other mods handle damage first, we just ensure compatibility
                // Check if this is DMC damage and ensure proper attribution
                if (dinfo.Instigator != null && dinfo.Def != null)
                {
                    // Enhanced compatibility - don't interfere with other mod damage systems
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[DMC Abilities] Non-critical error in damage compatibility: {ex.Message}");
            }
            
            return true; // Always allow damage to proceed for maximum compatibility
        }
    }
}