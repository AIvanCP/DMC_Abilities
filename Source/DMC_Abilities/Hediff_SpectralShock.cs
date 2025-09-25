using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace DMCAbilities
{
    /// <summary>
    /// Spectral Shock debuff from Rapid Slash summoned sword explosion
    /// Reduces manipulation -10%, increases aiming time +20%, lasts 3 seconds
    /// </summary>
    public class Hediff_SpectralShock : HediffWithComps
    {

        public override string LabelInBrackets
        {
            get
            {
                // Show remaining time in seconds
                int remainingTicks = Mathf.Max(0, 180 - ageTicks); // 3 seconds = 180 ticks
                float remainingSeconds = remainingTicks / 60f;
                return $"{remainingSeconds:F1}s";
            }
        }

        public override void Tick()
        {
            base.Tick();
            
            // Auto-remove after 3 seconds (180 ticks)
            if (ageTicks >= 180)
            {
                pawn?.health?.RemoveHediff(this);
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            
            // Visual effect when applied
            if (pawn?.Map != null)
            {
                FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.MicroSparks, 1.0f);
            }
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            
            // Visual effect when removed
            if (pawn?.Map != null)
            {
                FleckMaker.ThrowDustPuffThick(pawn.Position.ToVector3Shifted(), pawn.Map, 0.8f, Color.blue);
            }
        }
    }
}