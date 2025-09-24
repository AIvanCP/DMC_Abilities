using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using System.Collections.Generic;

namespace DMCAbilities
{
    public class Projectile_SpectralSword : Projectile
    {
        private int lifeTimeTicks = 300; // 5 seconds max lifetime
        private bool hasFallen = false;
        private Vector3 spawnPosition;
        private Vector3 targetPosition;
        private float fallSpeed = 2f;
        protected Pawn casterPawn;

        public void Initialize(Pawn caster, Vector3 target)
        {
            casterPawn = caster;
            targetPosition = target;
            spawnPosition = target + Vector3.up * 10f; // Spawn above target
            Position = spawnPosition.ToIntVec3();
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            IntVec3 impactCell = Position;

            if (map == null)
                return;

            // Create impact effects
            CreateImpactEffects(impactCell, map);

            // Find pawns to damage in the impact cell and adjacent cells
            List<Pawn> pawnsToHit = new List<Pawn>();
            
            // Check impact cell and adjacent cells for small AoE
            foreach (IntVec3 cell in GenAdjFast.AdjacentCells8Way(impactCell))
            {
                if (cell.InBounds(map))
                {
                    Pawn pawn = cell.GetFirstPawn(map);
                    if (pawn != null && pawn != casterPawn && !pawnsToHit.Contains(pawn))
                    {
                        pawnsToHit.Add(pawn);
                    }
                }
            }

            // Also check the impact cell itself
            Pawn centerPawn = impactCell.GetFirstPawn(map);
            if (centerPawn != null && centerPawn != casterPawn && !pawnsToHit.Contains(centerPawn))
            {
                pawnsToHit.Add(centerPawn);
            }

            // Apply damage to all found pawns
            foreach (Pawn pawn in pawnsToHit)
            {
                ApplySpectralSwordDamage(pawn);
            }

            base.Impact(hitThing, blockedByShield);
        }

        private void ApplySpectralSwordDamage(Pawn target)
        {
            if (target == null || target.Dead)
                return;

            // Calculate damage: 15-20 Cut/Pierce damage
            int baseDamage = Rand.Range(15, 21);
            DamageDef damageType = Rand.Bool ? DamageDefOf.Cut : DamageDefOf.Stab;

            DamageInfo damageInfo = new DamageInfo(
                def: damageType,
                amount: baseDamage,
                armorPenetration: 0.15f, // Small armor penetration
                angle: 0f,
                instigator: casterPawn,
                hitPart: null
            );

            target.TakeDamage(damageInfo);

            // Check if target already has spectral wound (for stun chance)
            bool hadSpectralWound = target.health.hediffSet.HasHediff(DMC_HediffDefOf.DMC_SpectralWound);

            // Apply Spectral Wound debuff
            ApplySpectralWound(target, false);

            // 15% chance to stun if already slowed
            if (hadSpectralWound && Rand.Chance(0.15f))
            {
                ApplySpectralStun(target, 90f); // 1.5 seconds
            }
        }

        protected void ApplySpectralWound(Pawn target, bool strongerVersion)
        {
            // Remove existing spectral wound to refresh duration
            Hediff existingWound = target.health.hediffSet.GetFirstHediffOfDef(DMC_HediffDefOf.DMC_SpectralWound);
            if (existingWound != null)
            {
                target.health.RemoveHediff(existingWound);
            }

            // Apply new spectral wound
            Hediff_SpectralWound spectralWound = (Hediff_SpectralWound)HediffMaker.MakeHediff(DMC_HediffDefOf.DMC_SpectralWound, target);
            spectralWound.SetStrongerVersion(strongerVersion);
            target.health.AddHediff(spectralWound);
        }

        protected void ApplySpectralStun(Pawn target, float durationTicks)
        {
            Hediff stunHediff = HediffMaker.MakeHediff(DMC_HediffDefOf.DMC_SpectralStun, target);
            stunHediff.ageTicks = 0;
            // Set custom duration by using a custom hediff implementation if needed
            target.health.AddHediff(stunHediff);
        }

        private void CreateImpactEffects(IntVec3 position, Map map)
        {
            // Impact visual effect
            FleckMaker.Static(position, map, FleckDefOf.PsycastAreaEffect, 1.5f);
            
            // Sparks effect
            for (int i = 0; i < 3; i++)
            {
                Vector2 sparkOffset2D = Rand.InsideUnitCircle * 0.5f;
                Vector3 sparkPos = position.ToVector3Shifted() + new Vector3(sparkOffset2D.x, 0, sparkOffset2D.y);
                FleckMaker.ThrowLightningGlow(sparkPos, map, 0.8f);
            }

            // Impact sound
            SoundStarter.PlayOneShot(SoundDefOf.Recipe_Surgery, new TargetInfo(position, map));
        }

        protected override void Tick()
        {
            base.Tick();

            if (!hasFallen)
            {
                // Move the projectile downward toward target
                Vector3 currentPos = Position.ToVector3();
                Vector3 newPos = Vector3.MoveTowards(currentPos, targetPosition, fallSpeed * 0.016f); // Approximate tick time

                if (Vector3.Distance(newPos, targetPosition) < 0.1f)
                {
                    // Reached target, trigger impact
                    hasFallen = true;
                    Impact(null, false);
                }
                else
                {
                    Position = newPos.ToIntVec3();
                }
            }

            // Cleanup after lifetime
            lifeTimeTicks--;
            if (lifeTimeTicks <= 0)
            {
                Destroy();
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (def.graphicData?.Graphic != null)
            {
                Vector3 drawPos = drawLoc;
                drawPos.y = AltitudeLayer.Projectile.AltitudeFor();
                
                // Add a slight rotation for visual effect
                float rotation = (300 - lifeTimeTicks) * 2f;
                
                def.graphicData.Graphic.Draw(drawPos, Rot4.North, this, rotation);
            }
        }
    }

    public class Projectile_SpectralSwordSpecial : Projectile_SpectralSword
    {
        private float aoERadius = 4f;

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            IntVec3 impactCell = Position;

            if (map == null)
                return;

            // Create enhanced impact effects for special sword
            CreateSpecialImpactEffects(impactCell, map);

            // Find all pawns within AoE radius (3-5 cells)
            List<Pawn> pawnsToHit = new List<Pawn>();
            
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(impactCell, aoERadius, true))
            {
                if (cell.InBounds(map))
                {
                    Pawn pawn = cell.GetFirstPawn(map);
                    if (pawn != null && pawn != casterPawn && !pawnsToHit.Contains(pawn))
                    {
                        pawnsToHit.Add(pawn);
                    }
                }
            }

            // Apply enhanced damage to all found pawns
            foreach (Pawn pawn in pawnsToHit)
            {
                ApplySpecialSpectralSwordDamage(pawn);
            }

            Destroy();
        }

        private void ApplySpecialSpectralSwordDamage(Pawn target)
        {
            if (target == null || target.Dead)
                return;

            // Calculate enhanced damage: 30-40 Cut/Pierce damage
            int baseDamage = Rand.Range(30, 41);
            DamageDef damageType = Rand.Bool ? DamageDefOf.Cut : DamageDefOf.Stab;

            DamageInfo damageInfo = new DamageInfo(
                def: damageType,
                amount: baseDamage,
                armorPenetration: 0.3f, // Higher armor penetration
                angle: 0f,
                instigator: casterPawn,
                hitPart: null
            );

            target.TakeDamage(damageInfo);

            // Apply stronger Spectral Wound (double duration)
            ApplySpectralWound(target, true);

            // Apply poison (toxic buildup)
            ApplyPoisonEffect(target);

            // Apply bleeding
            ApplyBleedingWound(target);

            // 20% chance to stun for 2 seconds
            if (Rand.Chance(0.20f))
            {
                ApplySpectralStun(target, 120f); // 2 seconds
            }
        }

        private void ApplyPoisonEffect(Pawn target)
        {
            // Add toxic buildup
            Hediff toxicBuildup = target.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup);
            if (toxicBuildup != null)
            {
                toxicBuildup.Severity += Rand.Range(0.05f, 0.1f);
            }
            else
            {
                Hediff newToxic = HediffMaker.MakeHediff(HediffDefOf.ToxicBuildup, target);
                newToxic.Severity = Rand.Range(0.05f, 0.1f);
                target.health.AddHediff(newToxic);
            }
        }

        private void ApplyBleedingWound(Pawn target)
        {
            // Apply a minor bleeding wound
            BodyPartRecord bodyPart = target.RaceProps.body.AllParts.RandomElement();
            if (bodyPart != null)
            {
                Hediff bleedingWound = HediffMaker.MakeHediff(HediffDefOf.Cut, target, bodyPart);
                bleedingWound.Severity = 5f; // Light bleeding
                target.health.AddHediff(bleedingWound, bodyPart);
            }
        }

        private void CreateSpecialImpactEffects(IntVec3 position, Map map)
        {
            // Large impact visual effect
            FleckMaker.Static(position, map, FleckDefOf.PsycastAreaEffect, 3.0f);
            
            // Multiple sparks and lightning effects
            for (int i = 0; i < 8; i++)
            {
                Vector2 sparkOffset2D = Rand.InsideUnitCircle * aoERadius * 0.5f;
                Vector3 sparkPos = position.ToVector3Shifted() + new Vector3(sparkOffset2D.x, 0, sparkOffset2D.y);
                FleckMaker.ThrowLightningGlow(sparkPos, map, 1.2f);
            }

            // Enhanced impact sound
            SoundStarter.PlayOneShot(SoundDefOf.Psycast_Skip_Pulse, new TargetInfo(position, map));
        }
    }
}