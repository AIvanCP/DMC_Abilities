using RimWorld;
using Verse;

namespace DMCAbilities
{
    public class Hediff_SpectralWound : HediffWithComps
    {
        private float baseDuration = 180f; // 3 seconds base duration
        private bool isStrongerVersion = false;
        private int durationTicks = 180;

        public void SetStrongerVersion(bool stronger)
        {
            isStrongerVersion = stronger;
            if (stronger)
            {
                // Double duration for stronger version
                durationTicks = (int)(baseDuration * 2f);
            }
            else
            {
                durationTicks = (int)(baseDuration + Rand.Range(0f, 120f)); // 3-5 seconds
            }

            // Extend duration for psychic sensitive pawns
            if (pawn?.psychicEntropy?.IsPsychicallySensitive == true)
            {
                durationTicks = (int)(durationTicks * 1.25f); // 25% longer
            }

            this.ageTicks = 0; // Reset age
        }

        public override void PostMake()
        {
            base.PostMake();
            
            // Set default duration if not already set
            if (durationTicks <= 0)
            {
                SetStrongerVersion(false);
            }
        }

        public override void Tick()
        {
            base.Tick();
            
            // Remove hediff when duration expires
            if (ageTicks >= durationTicks)
            {
                pawn.health.RemoveHediff(this);
            }
        }

        public override string TipStringExtra
        {
            get
            {
                string text = base.TipStringExtra;
                if (isStrongerVersion)
                {
                    text += "\n" + "DMC_SpectralWoundStronger".Translate();
                }
                return text;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref baseDuration, "baseDuration", 180f);
            Scribe_Values.Look(ref isStrongerVersion, "isStrongerVersion", false);
            Scribe_Values.Look(ref durationTicks, "durationTicks", 180);
        }
    }
}