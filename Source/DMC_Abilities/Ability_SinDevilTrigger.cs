using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace DMCAbilities
{
    public class Verb_SinDevilTrigger : Verb_CastAbility
    {
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            return true; // Always valid since we self-target
        }

        protected override bool TryCastShot()
        {
            // Check if mod and ability are enabled
            if (DMCAbilitiesMod.settings != null && 
                (!DMCAbilitiesMod.settings.modEnabled || !DMCAbilitiesMod.settings.sinDevilTriggerEnabled))
            {
                return false;
            }

            Pawn caster = CasterPawn;
            if (caster == null || caster.Map == null) return false;

            // Check if already in SDT state (but allow if only DT is active - SDT can upgrade from DT)
            if (caster.health.hediffSet.HasHediff(DMC_HediffDefOf.DMC_SinDevilTrigger))
            {
                Messages.Message("Sin Devil Trigger already active!", caster, MessageTypeDefOf.RejectInput);
                return false;
            }

            // Apply damage multiplier from settings
            float damageMultiplier = DMCAbilitiesMod.settings?.sinDevilTriggerDamageMultiplier ?? 2.0f;

            // Activate Sin Devil Trigger transformation
            ActivateSinDevilTrigger(caster, damageMultiplier);
            return true;
        }

        private void ActivateSinDevilTrigger(Pawn caster, float damageMultiplier)
        {
            try
            {
                // Create more dramatic visual effects than regular DT
                FleckMaker.Static(caster.Position, caster.Map, FleckDefOf.ExplosionFlash, 3.5f);
                FleckMaker.ThrowDustPuff(caster.Position.ToVector3Shifted(), caster.Map, 3.0f);
                
                // Multiple flash effects for ultimate transformation
                for (int i = 0; i < 3; i++)
                {
                    FleckMaker.Static(caster.Position, caster.Map, FleckDefOf.PsycastAreaEffect, 2.0f + i);
                }
                
                // Play dramatic transformation sound
                SoundDefOf.PsycastPsychicPulse?.PlayOneShot(new TargetInfo(caster.Position, caster.Map));

                // Show Sin Devil Trigger callout
                DMCSpeechUtility.TryShowCallout(caster, "DMC_SinDevilTriggerActivation", DMCAbilitiesMod.settings?.calloutChance ?? 75f);

                // Remove any existing Devil Trigger first
                Hediff existingDT = caster.health.hediffSet.GetFirstHediffOfDef(DMC_HediffDefOf.DMC_DevilTrigger);
                if (existingDT != null)
                {
                    caster.health.RemoveHediff(existingDT);
                }

                // Add Sin Devil Trigger hediff with superior enhancements
                Hediff_SinDevilTrigger sdtHediff = (Hediff_SinDevilTrigger)HediffMaker.MakeHediff(DMC_HediffDefOf.DMC_SinDevilTrigger, caster);
                sdtHediff.damageMultiplier = damageMultiplier;
                caster.health.AddHediff(sdtHediff);

                // Create massive visual effects in larger radius
                for (int i = 0; i < 16; i++)
                {
                    IntVec3 randomCell = caster.Position + GenRadial.RadialPattern[i + 1];
                    if (randomCell.InBounds(caster.Map))
                    {
                        FleckMaker.ThrowDustPuff(randomCell.ToVector3Shifted(), caster.Map, 1.5f);
                        if (i % 4 == 0) // Every 4th cell gets explosion flash
                        {
                            FleckMaker.Static(randomCell, caster.Map, FleckDefOf.ExplosionFlash, 1.0f);
                        }
                    }
                }

                // Massive screen shake for ultimate ability
                Find.CameraDriver.shaker.DoShake(2.0f);

                Messages.Message($"{caster.Name.ToStringShort} enters Sin Devil Trigger state! Ultimate power unleashed!", 
                    caster, MessageTypeDefOf.PositiveEvent);

                // Log activation for debugging
                if (Prefs.DevMode)
                {
                    Log.Message($"[DMC Abilities] {caster.Name?.ToStringShort} activated Sin Devil Trigger with {damageMultiplier}x multiplier");
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[DMC Abilities] Error activating Sin Devil Trigger: {ex}");
            }
        }
    }
}