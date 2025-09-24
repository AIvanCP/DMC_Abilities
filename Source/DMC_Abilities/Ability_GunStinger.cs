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

            // Perform safe teleport
            bool teleportSucceeded = WeaponDamageUtility.SafeTeleportPawn(caster, teleportPos, false);
            
            if (teleportSucceeded)
            {
                // Create visual effects
                CreateGunStingerEffects(caster.Position, target.Position);
            }

            // Apply shotgun damage at point-blank range
            ApplyGunStingerDamage(caster, target);

            return true;
        }

        private IntVec3 FindTeleportPosition(Pawn caster, Pawn target)
        {
            Map map = caster.Map;
            IntVec3 targetPos = target.Position;

            // Use the shared safe teleport utility to find a good position near the target
            return WeaponDamageUtility.FindSafeTeleportPosition(targetPos, map, caster, 3);
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
                // Apply full damage to primary target
                target.TakeDamage(damageInfo.Value);

                // 15% chance to apply burn effect to primary target
                if (Rand.Chance(0.15f))
                {
                    ApplyBurnEffect(target);
                }

                // Apply stun effect to primary target (0.5-1.5 seconds = 30-90 ticks)
                ApplyStunEffect(target);

                // Apply blast area damage to nearby enemies (shotgun spread effect)
                ApplyBlastAreaDamage(caster, target, damageInfo.Value);

                // Create impact effect using psycast effect with null safety
                if (target.Map != null)
                {
                    FleckMaker.Static(target.Position, target.Map, FleckDefOf.PsycastAreaEffect, 2.0f);
                    
                    // Additional muzzle flash effect
                    FleckMaker.ThrowLightningGlow(target.Position.ToVector3Shifted(), target.Map, 1.5f);
                }
            }
        }

        private void ApplyBlastAreaDamage(Pawn caster, Pawn primaryTarget, DamageInfo originalDamage)
        {
            Map map = primaryTarget.Map;
            IntVec3 targetPos = primaryTarget.Position;
            IntVec3 casterPos = caster.Position;

            // Calculate direction from caster to target for cone calculation
            Vector3 direction = (targetPos - casterPos).ToVector3().normalized;
            float coneAngle = 90f; // 90-degree cone like DMC5 Gun Stinger
            float maxRange = 3f; // 3-cell blast radius

            // Find all pawns within blast range
            List<Pawn> pawnsInRange = new List<Pawn>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(targetPos, maxRange, true))
            {
                if (!cell.InBounds(map) || cell == targetPos) // Skip primary target position
                    continue;

                Pawn pawn = cell.GetFirstPawn(map);
                if (pawn != null && pawn != caster && pawn != primaryTarget && pawn.HostileTo(caster))
                {
                    // Check if pawn is within the cone
                    Vector3 toPawn = (cell - casterPos).ToVector3().normalized;
                    float angle = Vector3.Angle(direction, toPawn);
                    
                    if (angle <= coneAngle / 2f) // Half-angle check for cone
                    {
                        pawnsInRange.Add(pawn);
                    }
                }
            }

            // Apply reduced damage to pawns in blast area
            foreach (Pawn pawn in pawnsInRange)
            {
                // Calculate distance falloff (closer = more damage)
                float distance = targetPos.DistanceTo(pawn.Position);
                float falloffMultiplier = Mathf.Lerp(0.7f, 0.3f, distance / maxRange); // 70% damage at close range, 30% at max range

                // Create reduced damage info
                DamageInfo blastDamage = new DamageInfo(
                    def: originalDamage.Def,
                    amount: (int)(originalDamage.Amount * falloffMultiplier),
                    armorPenetration: 0.15f, // Reduced armor penetration for blast damage
                    angle: originalDamage.Angle,
                    instigator: caster,
                    weapon: originalDamage.Weapon
                );

                pawn.TakeDamage(blastDamage);

                // Visual effect for blast victims
                FleckMaker.Static(pawn.Position, map, FleckDefOf.ShotFlash, 1.0f);
                
                // 5% chance for burn on blast targets (reduced from primary target)
                if (Rand.Chance(0.05f))
                {
                    ApplyBurnEffect(pawn);
                }
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
            return 3f; // Show blast range
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            // Draw the blast cone area behind the target
            if (target.IsValid && target.Pawn != null && CasterPawn != null)
            {
                IntVec3 targetPos = target.Cell;
                IntVec3 casterPos = CasterPawn.Position;
                
                // Calculate direction from caster to target for cone calculation
                Vector3 direction = (targetPos - casterPos).ToVector3().normalized;
                float coneAngle = 90f; // 90-degree cone
                float maxRange = 3f; // 3-cell blast radius

                // Find all cells within the blast cone
                List<IntVec3> coneCells = new List<IntVec3>();
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(targetPos, maxRange, true))
                {
                    if (!cell.InBounds(CasterPawn.Map) || cell == targetPos) 
                        continue;

                    // Check if cell is within the cone
                    Vector3 toCellDir = (cell - casterPos).ToVector3().normalized;
                    float angle = Vector3.Angle(direction, toCellDir);
                    
                    if (angle <= coneAngle / 2f) // Half-angle check for cone
                    {
                        coneCells.Add(cell);
                    }
                }

                // Draw the cone area
                if (coneCells.Count > 0)
                {
                    GenDraw.DrawFieldEdges(coneCells);
                }
                
                // Also highlight the primary target
                GenDraw.DrawTargetHighlight(target);
            }
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