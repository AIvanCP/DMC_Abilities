using Verse;

namespace DMCAbilities
{
    public class Hediff_GunStinger : HediffWithComps
    {
        public override void PostMake()
        {
            base.PostMake();
            Log.Message($"[DMC] Gun Stinger hediff applied to {pawn?.Name}");
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            Log.Message($"[DMC] Gun Stinger hediff removed from {pawn?.Name}");
        }
    }
}