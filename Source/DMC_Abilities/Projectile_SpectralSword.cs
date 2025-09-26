using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using System.Collections.Generic;
using System.Linq;

namespace DMCAbilities
{
    public class Projectile_SpectralSword : Projectile
    {
        private int lifeTimeTicks = 300; // 5 seconds max lifetime
        protected Pawn casterPawn;

        public void Initialize(Pawn caster, Vector3 target)
        {
            casterPawn = caster;
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
            SoundStarter.PlayOneShot(SoundDefOf.Pawn_Melee_Punch_HitPawn, new TargetInfo(position, map));
        }

        protected override void Tick()
        {
            base.Tick();

            // Cleanup after lifetime
            lifeTimeTicks--;
            if (lifeTimeTicks <= 0)
            {
                Destroy();
                return;
            }

            // Let the base projectile handle movement towards the destination
            // The projectile will automatically impact when it reaches its destination
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
            // Apply a minor bleeding wound to an existing body part
            List<BodyPartRecord> availableParts = target.health.hediffSet.GetNotMissingParts().ToList();
            if (availableParts.Count > 0)
            {
                BodyPartRecord bodyPart = availableParts.RandomElement();
                if (bodyPart != null)
                {
                    Hediff bleedingWound = HediffMaker.MakeHediff(HediffDefOf.Cut, target, bodyPart);
                    bleedingWound.Severity = 5f; // Light bleeding
                    target.health.AddHediff(bleedingWound, bodyPart);
                }
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

    /// <summary>
    /// Rapid Slash spectral sword that explodes after delay with AOE damage and Spectral Shock debuff
    /// </summary>
    public class Projectile_SpectralSwordRapidSlash : Projectile
    {
        private IntVec3 explosionTarget;
        private Pawn originalCaster;
        private int ticksToExplode = 30; // 0.5 seconds at 60 ticks/second
        private bool hasExploded = false;

        public void SetExplosionTarget(IntVec3 target, Pawn caster)
        {
            explosionTarget = target;
            originalCaster = caster;
        }

        protected override void Tick()
        {
            base.Tick();

            if (hasExploded)
                return;

            ticksToExplode--;

            // Visual warning effect as it gets close to explosion
            if (ticksToExplode <= 15 && ticksToExplode % 5 == 0)
            {
                FleckMaker.Static(Position, Map, FleckDefOf.MicroSparks, 0.8f);
            }

            // Explode after delay
            if (ticksToExplode <= 0)
            {
                ExplodeSpectralSword();
                hasExploded = true;
            }
        }

        private void ExplodeSpectralSword()
        {
            Map map = Map;
            if (map == null) return;

            IntVec3 center = explosionTarget.IsValid ? explosionTarget : Position;

            // Create explosion visual effects
            FleckMaker.Static(center, map, FleckDefOf.ExplosionFlash, 2.0f);
            FleckMaker.ThrowDustPuffThick(center.ToVector3Shifted(), map, 2.0f, UnityEngine.Color.cyan);

            // Sound effect
            SoundStarter.PlayOneShot(SoundDefOf.Psycast_Skip_Pulse, new TargetInfo(center, map));

            // Find all targets in radius 2 AOE
            List<IntVec3> affectedCells = new List<IntVec3>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 2, true))
            {
                if (cell.InBounds(map))
                {
                    affectedCells.Add(cell);
                }
            }

            // Apply damage to all valid targets in AOE
            foreach (IntVec3 cell in affectedCells)
            {
                List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
                // Create a copy to prevent collection modification errors
                List<Thing> thingsCopy = new List<Thing>(things);
                foreach (Thing thing in thingsCopy)
                {
                    // Target pawns, animals, mechs, and turrets
                    if ((thing is Pawn targetPawn && targetPawn != originalCaster && !targetPawn.Dead) ||
                        (thing.def.building?.IsTurret == true))
                    {
                        ApplySpectralExplosionDamage(thing);
                    }
                }
            }

            // Destroy the projectile
            Destroy();
        }

        private void ApplySpectralExplosionDamage(Thing target)
        {
            if (target == null || target.Destroyed) return;

            // AOE damage: 8-12 cut/pierce damage
            float damage = Rand.Range(8f, 12f);
            
            DamageInfo damageInfo = new DamageInfo(
                DamageDefOf.Cut,
                damage,
                0f,
                -1f,
                originalCaster,
                null,
                null,
                DamageInfo.SourceCategory.ThingOrUnknown
            );

            target.TakeDamage(damageInfo);

            // Apply Spectral Shock debuff to pawns only
            if (target is Pawn pawnTarget && pawnTarget.health?.hediffSet != null)
            {
                ApplySpectralShock(pawnTarget);
            }

            // Visual effect at impact
            FleckMaker.ThrowDustPuffThick(target.Position.ToVector3Shifted(), Map, 0.8f, UnityEngine.Color.blue);

            Log.Message($"Spectral Sword Explosion: Hit {target.Label} for {damage:F1} damage");
        }

        private void ApplySpectralShock(Pawn target)
        {
            // Remove existing spectral shock to refresh duration
            Hediff existingShock = target.health.hediffSet.GetFirstHediffOfDef(DMC_HediffDefOf.DMC_SpectralShock);
            if (existingShock != null)
            {
                target.health.RemoveHediff(existingShock);
            }

            // Add Spectral Shock debuff: -10% manipulation, +20% aiming time, 3 seconds
            Hediff spectralShock = HediffMaker.MakeHediff(DMC_HediffDefOf.DMC_SpectralShock, target);
            spectralShock.Severity = 1.0f;
            target.health.AddHediff(spectralShock);

            Log.Message($"Applied Spectral Shock to {target.Label}");
        }
    }
}