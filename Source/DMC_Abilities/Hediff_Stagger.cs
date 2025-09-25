using RimWorld;
using Verse;
using UnityEngine;

namespace DMCAbilities
{
    public class Hediff_Stagger : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            ApplyStaggerEffect();
        }

        public override void Tick()
        {
            base.Tick();
            
            // Auto-remove stagger after 2 seconds (120 ticks)
            if (ageTicks >= 120)
            {
                if (pawn?.health != null)
                    pawn.health.RemoveHediff(this);
            }
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

        public override string LabelInBrackets => $"{Mathf.Max(0f, (120 - ageTicks) / 60f):F1}s";
    }
}