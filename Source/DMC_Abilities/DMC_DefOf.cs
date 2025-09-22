using RimWorld;
using Verse;

namespace DMCAbilities
{
    [DefOf]
    public static class DMC_JobDefOf
    {
        public static JobDef DMC_JudgementCutWarmup;

        static DMC_JobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DMC_JobDefOf));
        }
    }

    [DefOf]
    public static class DMC_HediffDefOf
    {
        public static HediffDef DMC_StingerAbility;
        public static HediffDef DMC_JudgementCutAbility;

        static DMC_HediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DMC_HediffDefOf));
        }
    }

    [DefOf]
    public static class DMC_AbilityDefOf
    {
        public static AbilityDef DMC_Stinger;
        public static AbilityDef DMC_JudgementCut;

        static DMC_AbilityDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DMC_AbilityDefOf));
        }
    }
}