using RimWorld;
using Verse;

namespace DMCAbilities
{
    /// <summary>
    /// Utility for directly granting DMC abilities to pawns without using hediffs.
    /// This mimics vanilla psycast ability system.
    /// </summary>
    public static class DMCAbilityUtility
    {
        /// <summary>
        /// Attempts to unlock an ability for a pawn. Returns false if already known.
        /// </summary>
        public static bool TryUnlockAbility(Pawn pawn, string abilityDefName, out string abilityLabel)
        {
            abilityLabel = abilityDefName;

            if (pawn?.abilities == null)
                return false;

            AbilityDef abilityDef = DefDatabase<AbilityDef>.GetNamedSilentFail(abilityDefName);
            if (abilityDef == null)
            {
                Log.Error($"[DMC Abilities] Could not find AbilityDef: {abilityDefName}");
                return false;
            }

            abilityLabel = abilityDef.label;

            // Check if already has this ability
            if (pawn.abilities.GetAbility(abilityDef) != null)
            {
                return false; // Already known
            }

            // Grant the ability directly
            pawn.abilities.GainAbility(abilityDef);
            
            return true;
        }

        /// <summary>
        /// Checks if a pawn has a specific ability.
        /// </summary>
        public static bool HasAbility(Pawn pawn, string abilityDefName, out string abilityLabel)
        {
            abilityLabel = abilityDefName;

            if (pawn?.abilities == null)
                return false;

            AbilityDef abilityDef = DefDatabase<AbilityDef>.GetNamedSilentFail(abilityDefName);
            if (abilityDef == null)
                return false;

            abilityLabel = abilityDef.label;
            return pawn.abilities.GetAbility(abilityDef) != null;
        }

        /// <summary>
        /// Removes an ability from a pawn.
        /// </summary>
        public static void RemoveAbility(Pawn pawn, string abilityDefName)
        {
            if (pawn?.abilities == null)
                return;

            AbilityDef abilityDef = DefDatabase<AbilityDef>.GetNamedSilentFail(abilityDefName);
            if (abilityDef == null)
                return;

            Ability ability = pawn.abilities.GetAbility(abilityDef);
            if (ability != null)
            {
                pawn.abilities.RemoveAbility(abilityDef);
            }
        }
    }
}
