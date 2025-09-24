using RimWorld;
using Verse;

namespace DMCAbilities
{
    [DefOf]
    public static class DMC_JobDefOf
    {
        public static JobDef DMC_DriveDelayedLaunch;
        public static JobDef DMC_HeavyRainCast;
        public static JobDef DMC_RainBulletCast;

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
        public static HediffDef DMC_HeavyRainAbility;
        public static HediffDef DMC_RainBulletAbility;
        public static HediffDef DMC_SpectralWound;
        public static HediffDef DMC_SpectralStun;
        public static HediffDef DMC_Stagger;

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
        public static AbilityDef DMC_HeavyRain;
        public static AbilityDef DMC_RainBullet;

        static DMC_AbilityDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DMC_AbilityDefOf));
        }
    }

    [DefOf]
    public static class DMC_ThingDefOf
    {
        public static ThingDef DMC_DriveSlashProjectile;
        public static ThingDef DMC_SpectralSwordProjectile;
        public static ThingDef DMC_SpectralSwordSpecialProjectile;
        public static ThingDef DMC_RainBulletProjectile;

        static DMC_ThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DMC_ThingDefOf));
        }
    }
}