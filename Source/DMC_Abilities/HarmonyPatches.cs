using HarmonyLib;
using RimWorld;
using Verse;
using System;
using UnityEngine;

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

        public static void Postfix(Thing __instance, DamageInfo dinfo, ref DamageWorker.DamageResult __result)
        {
            try
            {
                // Handle cooldown reduction for Devil Trigger abilities
                if (dinfo.Instigator is Pawn attacker && __result.totalDamageDealt > 0)
                {
                    ReduceDevilTriggerCooldowns(attacker, 0.1f); // 0.1 seconds per damage dealt
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[DMC Abilities] Non-critical error in damage cooldown reduction: {ex.Message}");
            }
        }
        
        private static void ReduceDevilTriggerCooldowns(Pawn pawn, float reduction)
        {
            if (pawn?.abilities?.abilities == null) return;
            
            foreach (var ability in pawn.abilities.abilities)
            {
                if (ability.def.defName == "DMC_DevilTrigger" || ability.def.defName == "DMC_SinDevilTrigger")
                {
                    if (ability.CooldownTicksRemaining > 0)
                    {
                        int tickReduction = Mathf.FloorToInt(reduction * 60f); // Convert to ticks
                        ability.StartCooldown(Math.Max(0, ability.CooldownTicksRemaining - tickReduction));
                    }
                }
            }
        }
    }

    // Patch for kill-based cooldown reduction
    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Pawn_Kill_Patch
    {
        public static bool Prepare()
        {
            return typeof(Pawn).GetMethod("Kill", new[] { typeof(DamageInfo?), typeof(Hediff) }) != null;
        }

        public static void Postfix(Pawn __instance, DamageInfo? dinfo)
        {
            try
            {
                // Handle cooldown reduction when a pawn is killed by Devil Trigger user
                if (dinfo.HasValue && dinfo.Value.Instigator is Pawn killer)
                {
                    ReduceDevilTriggerCooldowns(killer, 0.5f); // 0.5 seconds per kill
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[DMC Abilities] Non-critical error in kill cooldown reduction: {ex.Message}");
            }
        }
        
        private static void ReduceDevilTriggerCooldowns(Pawn pawn, float reduction)
        {
            if (pawn?.abilities?.abilities == null) return;
            
            foreach (var ability in pawn.abilities.abilities)
            {
                if (ability.def.defName == "DMC_DevilTrigger" || ability.def.defName == "DMC_SinDevilTrigger")
                {
                    if (ability.CooldownTicksRemaining > 0)
                    {
                        int tickReduction = Mathf.FloorToInt(reduction * 60f); // Convert to ticks
                        ability.StartCooldown(Math.Max(0, ability.CooldownTicksRemaining - tickReduction));
                    }
                }
            }
        }
    }

    // Enhanced ability settings patch to include Devil Trigger abilities
    [HarmonyPatch(typeof(Ability), "get_Disabled")]
    public static class DevilTrigger_Disabled_Patch
    {
        public static bool Prepare()
        {
            return typeof(Ability).GetProperty("Disabled") != null;
        }

        public static void Postfix(Ability __instance, ref bool __result)
        {
            try
            {
                if (__instance?.def?.defName == null || DMCAbilitiesMod.settings == null) return;
                
                if (!__instance.def.defName.StartsWith("DMC_")) return;

                if (!DMCAbilitiesMod.settings.modEnabled)
                {
                    __result = true;
                    return;
                }

                // Additional checks for Devil Trigger abilities
                if (__instance.def.defName == "DMC_DevilTrigger" && !DMCAbilitiesMod.settings.devilTriggerEnabled)
                {
                    __result = true;
                }
                else if (__instance.def.defName == "DMC_SinDevilTrigger" && !DMCAbilitiesMod.settings.sinDevilTriggerEnabled)
                {
                    __result = true;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[DMC Abilities] Non-critical error in Devil Trigger settings check: {ex.Message}");
            }
        }
    }
}