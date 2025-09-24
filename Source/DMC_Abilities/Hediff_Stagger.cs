using RimWorld;
using Verse;

namespace DMCAbilities
{
    public class Hediff_Stagger : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            ApplyStaggerEffect();
        }

        private void ApplyStaggerEffect()
        {
            if (pawn?.health == null || pawn.Dead || pawn.Map == null)
                return;

            // Try to find appropriate stagger/stun effect
            HediffDef stunDef = DefDatabase<HediffDef>.GetNamedSilentFail("Stun") ?? 
                               DefDatabase<HediffDef>.GetNamedSilentFail("Unconscious") ??
                               HediffDefOf.Anesthetic;

            if (stunDef != null)
            {
                Hediff stunHediff = HediffMaker.MakeHediff(stunDef, pawn);
                stunHediff.Severity = 0.2f; // Light stagger effect
                pawn.health.AddHediff(stunHediff);
            }

            // Visual effect - small impact flash
            if (pawn.Map != null)
            {
                FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.ExplosionFlash, 0.8f);
            }
        }

        public override string LabelInBrackets => $"{(120 - ageTicks) / 60f:F1}s";
    }
}