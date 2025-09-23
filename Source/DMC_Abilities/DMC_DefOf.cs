using RimWorld;
using Verse;

namespace DMCAbilities
{
    [DefOf]
    public static class DMC_JobDefOf
    {
        public static JobDef DMC_DriveDelayedLaunch;

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
        public static HediffDef DMC_DriveAbility;
        public static HediffDef DMC_DriveBurn;
        public static HediffDef DMC_VoidSlashAbility;
        public static HediffDef DMC_VoidSlashDebuff;
        public static HediffDef DMC_GunStingerHediff;

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
        public static AbilityDef DMC_Drive;
        public static AbilityDef DMC_VoidSlash;
        public static AbilityDef DMC_GunStinger;

        static DMC_AbilityDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DMC_AbilityDefOf));
        }
    }

    [DefOf]
    public static class DMC_ThingDefOf
    {
        public static ThingDef DMC_DriveSlashProjectile;

        static DMC_ThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DMC_ThingDefOf));
        }
    }
}