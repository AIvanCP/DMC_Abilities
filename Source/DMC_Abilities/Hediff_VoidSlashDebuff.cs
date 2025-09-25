using RimWorld;
using Verse;
using UnityEngine;

namespace DMCAbilities
{
    public class Hediff_VoidSlashDebuff : HediffWithComps
    {
        private int ticksSinceLastBleed = 0;
        private const int BleedTickInterval = 60; // Bleed every 60 ticks (1 second)
        private const float BleedDamagePerSecond = 0.5f;
        private bool hasStaggered = false;

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            
            // 10% chance to stagger on initial application
            if (Rand.Chance(0.1f) && pawn != null && !hasStaggered)
            {
                ApplyStagger();
                hasStaggered = true;
            }
        }

        public override void Tick()
        {
            base.Tick();
            
            // Null safety checks
            if (pawn == null || pawn.Dead || pawn.health == null)
            {
                // Remove the hediff if pawn is invalid
                if (pawn?.health != null)
                    pawn.health.RemoveHediff(this);
                return;
            }
            
            // Apply bleeding damage every second
            ticksSinceLastBleed++;
            if (ticksSinceLastBleed >= BleedTickInterval)
            {
                ApplyBleedingDamage();
                ticksSinceLastBleed = 0;
            }

            // Auto-remove after duration (5-8 seconds = 300-480 ticks)
            if (ageTicks >= 400) // 6.67 seconds average
            {
                pawn.health.RemoveHediff(this);
            }
        }

        private void ApplyBleedingDamage()
        {
            if (pawn?.health == null || pawn.Dead)
                return;

            DamageInfo bleedDamage = new DamageInfo(
                def: DamageDefOf.Cut,
                amount: BleedDamagePerSecond,
                armorPenetration: 0f,
                angle: 0f,
                instigator: null
            );

            pawn.TakeDamage(bleedDamage);
        }

        private void ApplyStagger()
        {
            if (pawn?.health == null || pawn.Dead || pawn.Map == null)
                return;

            // Try to find a stun-like hediff, or create a brief unconscious effect
            HediffDef stunDef = DefDatabase<HediffDef>.GetNamedSilentFail("Stun") ?? 
                               DefDatabase<HediffDef>.GetNamedSilentFail("Unconscious") ??
                               HediffDefOf.Anesthetic;

            if (stunDef != null)
            {
                Hediff stunHediff = HediffMaker.MakeHediff(stunDef, pawn);
                stunHediff.Severity = 0.3f; // Light stun
                pawn.health.AddHediff(stunHediff);
            }

            // Visual effect with null safety
            if (pawn.Map != null)
            {
                FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect, 1.5f);
            }
        }

        public override string LabelInBrackets => $"{Mathf.Max(0f, (400 - ageTicks) / 60f):F1}s";
    }
}