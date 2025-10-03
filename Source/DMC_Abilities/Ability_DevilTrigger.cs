using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace DMCAbilities
{
    public class Verb_DevilTrigger : Verb_CastAbility
    {
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            return true; // Always valid since we self-target
        }

        protected override bool TryCastShot()
        {
            // Check if mod and ability are enabled
            if (DMCAbilitiesMod.settings != null && 
                (!DMCAbilitiesMod.settings.modEnabled || !DMCAbilitiesMod.settings.devilTriggerEnabled))
            {
                return false;
            }

            Pawn caster = CasterPawn;
            if (caster == null || caster.Map == null) return false;

            // Check if already in any transformation state
            if (caster.health.hediffSet.HasHediff(DMC_HediffDefOf.DMC_DevilTrigger))
            {
                Messages.Message("Devil Trigger already active!", caster, MessageTypeDefOf.RejectInput);
                return false;
            }
            
            if (caster.health.hediffSet.HasHediff(DMC_HediffDefOf.DMC_SinDevilTrigger))
            {
                Messages.Message("Sin Devil Trigger is active! Cannot downgrade to regular Devil Trigger.", caster, MessageTypeDefOf.RejectInput);
                return false;
            }

            // Apply damage multiplier from settings
            float damageMultiplier = DMCAbilitiesMod.settings?.devilTriggerDamageMultiplier ?? 1.5f;

            // Activate Devil Trigger transformation
            ActivateDevilTrigger(caster, damageMultiplier);
            return true;
        }

        private void ActivateDevilTrigger(Pawn caster, float damageMultiplier)
        {
            try
            {
                // Create visual and audio effects
                FleckMaker.Static(caster.Position, caster.Map, FleckDefOf.ExplosionFlash, 2.5f);
                FleckMaker.ThrowDustPuff(caster.Position.ToVector3Shifted(), caster.Map, 2.0f);
                
                // Play transformation sound if available
                SoundDefOf.PsycastPsychicEffect?.PlayOneShot(new TargetInfo(caster.Position, caster.Map));

                // Show Devil Trigger callout
                DMCSpeechUtility.TryShowCallout(caster, "DMC_DevilTriggerActivation", DMCAbilitiesMod.settings?.calloutChance ?? 75f);

                // Add Devil Trigger hediff with enhanced stats
                Hediff_DevilTrigger dtHediff = (Hediff_DevilTrigger)HediffMaker.MakeHediff(DMC_HediffDefOf.DMC_DevilTrigger, caster);
                dtHediff.damageMultiplier = damageMultiplier;
                caster.health.AddHediff(dtHediff);

                // Create dramatic visual effects around caster
                for (int i = 0; i < 8; i++)
                {
                    IntVec3 randomCell = caster.Position + GenRadial.RadialPattern[i + 1];
                    if (randomCell.InBounds(caster.Map))
                    {
                        FleckMaker.ThrowDustPuff(randomCell.ToVector3Shifted(), caster.Map, 1.0f);
                    }
                }

                // Screen flash effect for dramatic impact
                Find.CameraDriver.shaker.DoShake(1.0f);

                Messages.Message($"{caster.Name.ToStringShort} enters Devil Trigger state! Enhanced combat capabilities activated.", 
                    caster, MessageTypeDefOf.PositiveEvent);

                // Log activation for debugging
                if (Prefs.DevMode)
                {
                    Log.Message($"[DMC Abilities] {caster.Name?.ToStringShort} activated Devil Trigger with {damageMultiplier}x multiplier");
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[DMC Abilities] Error activating Devil Trigger: {ex}");
            }
        }
    }
}