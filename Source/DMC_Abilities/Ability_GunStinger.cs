using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using System.Collections.Generic;
using System.Linq;

namespace DMCAbilities
{
    public class Verb_GunStinger : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            // Check if mod and ability are enabled
            if (DMCAbilitiesMod.settings != null && 
                (!DMCAbilitiesMod.settings.modEnabled || !DMCAbilitiesMod.settings.gunStingerEnabled))
            {
                return false;
            }

            // Check for shotgun weapon requirement
            if (!WeaponDamageUtility.HasShotgunWeapon(CasterPawn))
            {
                Messages.Message(WeaponDamageUtility.GetNoShotgunWeaponMessage(), 
                    CasterPawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            Pawn target = currentTarget.Pawn;
            if (target == null)
                return false;

            return CastGunStinger(target);
        }

        private bool CastGunStinger(Pawn target)
        {
            Pawn caster = CasterPawn;
            if (caster == null || target == null || target.Dead)
                return false;

            // Find teleport position near target
            IntVec3 teleportPos = FindTeleportPosition(caster, target);
            if (teleportPos == IntVec3.Invalid)
            {
                Messages.Message("Cannot reach target.", caster, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // Perform teleport
            TeleportPawn(caster, teleportPos);

            // Create visual effects
            CreateGunStingerEffects(caster.Position, target.Position);

            // Apply shotgun damage at point-blank range
            ApplyGunStingerDamage(caster, target);

            return true;
        }

        private IntVec3 FindTeleportPosition(Pawn caster, Pawn target)
        {
            Map map = caster.Map;
            IntVec3 targetPos = target.Position;

            // Try adjacent cells around target
            List<IntVec3> adjacentCells = new List<IntVec3>();
            
            foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(target))
            {
                if (cell.InBounds(map) && cell.Standable(map) && 
                    cell.GetFirstPawn(map) == null && caster.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
                {
                    adjacentCells.Add(cell);
                }
            }

            if (adjacentCells.Count == 0)
                return IntVec3.Invalid;

            // Return closest cell to caster
            return adjacentCells.OrderBy(c => c.DistanceTo(caster.Position)).First();
        }

        private void TeleportPawn(Pawn pawn, IntVec3 destination)
        {
            if (pawn?.Map == null || !destination.IsValid)
                return;

            Map map = pawn.Map;
            
            // Create departure effect
            FleckMaker.ThrowDustPuff(pawn.Position.ToVector3Shifted(), map, 1.5f);

            // Teleport with proper notification
            pawn.Position = destination;
            pawn.Notify_Teleported(false, true);

            // Create arrival effect
            FleckMaker.ThrowDustPuff(destination.ToVector3Shifted(), map, 1.5f);
        }

        private void CreateGunStingerEffects(IntVec3 casterPos, IntVec3 targetPos)
        {
            Map map = CasterPawn?.Map;
            if (map == null)
                return;

            // Create teleport effect sound
            SoundStarter.PlayOneShot(SoundDefOf.Psycast_Skip_Entry, new TargetInfo(casterPos, map));
            
            // Create enhanced visual effects at both positions
            FleckMaker.Static(casterPos, map, FleckDefOf.PsycastAreaEffect, 2.5f);
            FleckMaker.Static(targetPos, map, FleckDefOf.PsycastAreaEffect, 2.5f);
        }

        private void ApplyGunStingerDamage(Pawn caster, Pawn target)
        {
            // Null safety checks
            if (caster == null || target == null || target.Dead || target.Map == null)
                return;

            // Check if Gun Stinger is enabled in settings
            if (DMCAbilitiesMod.settings != null && !DMCAbilitiesMod.settings.gunStingerEnabled)
                return;

            // Calculate damage with 1.5x multiplier
            float multiplier = DMCAbilitiesMod.settings?.gunStingerDamageMultiplier ?? 1.5f;
            var damageInfo = WeaponDamageUtility.CalculateRangedDamage(caster, multiplier);
            if (damageInfo.HasValue)
            {
                // Apply damage
                target.TakeDamage(damageInfo.Value);

                // 15% chance to apply burn effect
                if (Rand.Chance(0.15f))
                {
                    ApplyBurnEffect(target);
                }

                // Create impact effect using psycast effect with null safety
                if (target.Map != null)
                {
                    FleckMaker.Static(target.Position, target.Map, FleckDefOf.PsycastAreaEffect, 2.0f);
                    
                    // Additional muzzle flash effect
                    FleckMaker.ThrowLightningGlow(target.Position.ToVector3Shifted(), target.Map, 1.5f);
                }

                // Apply stun effect (0.5-1.5 seconds = 30-90 ticks)
                ApplyStunEffect(target);
            }
        }

        private void ApplyBurnEffect(Pawn target)
        {
            if (target?.health == null || target.Dead || target.Map == null)
                return;

            // Apply burn injury - use either Burns or ThermalBurn hediff
            HediffDef burnDef = DefDatabase<HediffDef>.GetNamedSilentFail("Burn") ?? 
                               DefDatabase<HediffDef>.GetNamedSilentFail("Burns") ??
                               DefDatabase<HediffDef>.GetNamedSilentFail("ThermalBurn");

            if (burnDef != null)
            {
                // Add burn to a random body part (like torso, arms, legs)
                BodyPartRecord targetPart = target.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Burn);
                if (targetPart != null)
                {
                    Hediff burnHediff = HediffMaker.MakeHediff(burnDef, target, targetPart);
                    burnHediff.Severity = Rand.Range(0.15f, 0.35f); // Minor to moderate burn
                    target.health.AddHediff(burnHediff, targetPart);
                    
                    // Visual effect for burn
                    if (target.Map != null)
                    {
                        FleckMaker.ThrowFireGlow(target.Position.ToVector3Shifted(), target.Map, 1.0f);
                    }
                }
            }
        }

        private void ApplyStunEffect(Pawn target)
        {
            if (target?.health == null || target.Dead || target.Map == null)
                return;

            // Try to find a stun-like hediff, or use anesthetic as fallback
            HediffDef stunDef = DefDatabase<HediffDef>.GetNamedSilentFail("Stun") ?? 
                               DefDatabase<HediffDef>.GetNamedSilentFail("Unconscious") ??
                               HediffDefOf.Anesthetic;

            if (stunDef != null)
            {
                Hediff stunHediff = HediffMaker.MakeHediff(stunDef, target);
                stunHediff.Severity = 0.2f; // Light stun (0.5-1.5 seconds worth)
                target.health.AddHediff(stunHediff);
                
                // Visual effect for stun
                if (target.Map != null)
                {
                    FleckMaker.Static(target.Position, target.Map, FleckDefOf.PsycastAreaEffect, 1.0f);
                }
            }
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = true;
            return 0f;
        }

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.ValidateTarget(target, showMessages))
                return false;

            if (target.Pawn == null)
            {
                if (showMessages)
                    Messages.Message("Must target a living creature.", CasterPawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (target.Pawn == CasterPawn)
            {
                if (showMessages)
                    Messages.Message("Cannot target yourself.", CasterPawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return true;
        }
    }
}