using HarmonyLib;
using RimWorld;
using Verse;

namespace DMCAbilities
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("dmcabilities.rimworld.devilmaycry");
            harmony.PatchAll();
            
            Log.Message("[DMC Abilities] Harmony patches applied successfully.");
        }
    }

    // Patch to ensure our abilities don't conflict with other ability mods
    [HarmonyPatch(typeof(Pawn_AbilityTracker), "GainAbility")]
    public static class Pawn_AbilityTracker_GainAbility_Patch
    {
        public static void Postfix(Pawn_AbilityTracker __instance, AbilityDef def)
        {
            // Log when DMC abilities are gained for debugging
            if (def != null && def.defName != null && def.defName.StartsWith("DMC_"))
            {
                Log.Message($"[DMC Abilities] Pawn {__instance.pawn?.Name?.ToStringShort ?? "Unknown"} gained ability: {def.defName}");
            }
        }
    }

    // Patch to handle mod settings for ability availability
    [HarmonyPatch(typeof(Ability), "get_Disabled")]
    public static class Ability_Disabled_Patch
    {
        public static void Postfix(Ability __instance, ref bool __result)
        {
            // If it's one of our abilities, check mod settings
            if (__instance?.def?.defName != null && __instance.def.defName.StartsWith("DMC_"))
            {
                if (DMCAbilitiesMod.settings == null || !DMCAbilitiesMod.settings.modEnabled)
                {
                    __result = true;
                    return;
                }

                // Check specific ability settings
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
        }
    }

    // Compatibility patch for weapon mods that might change melee verb detection
    [HarmonyPatch(typeof(CompEquippable), "get_AllVerbs")]
    public static class CompEquippable_AllVerbs_Patch
    {
        public static void Postfix(CompEquippable __instance, ref System.Collections.Generic.List<Verb> __result)
        {
            // Ensure our weapon utility can always access verbs properly
            if (__result == null)
            {
                __result = new System.Collections.Generic.List<Verb>();
            }
        }
    }
}