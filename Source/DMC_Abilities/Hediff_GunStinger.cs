using Verse;

namespace DMCAbilities
{
    public class Hediff_GunStinger : HediffWithComps
    {
        public override void PostMake()
        {
            base.PostMake();
            // Gun Stinger hediff applied
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            // Gun Stinger hediff removed
        }
    }
}