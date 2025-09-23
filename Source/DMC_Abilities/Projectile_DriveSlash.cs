using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DMCAbilities
{
    public class Projectile_DriveSlash : Projectile
    {
        private bool isFinalProjectile = false;
        private float extraDamageMultiplier = 1f;
        private int continuousDamageTicks = 0;
        private List<Pawn> hitPawns = new List<Pawn>();
        private const int DamageTickInterval = 10; // Apply damage every 10 ticks

        public void SetFinalProjectile(bool isFinal, float damageMultiplier = 2f)
        {
            isFinalProjectile = isFinal;
            extraDamageMultiplier = damageMultiplier;
            if (isFinal)
            {
                // Final projectile gets enhanced effects and continuous damage
                continuousDamageTicks = 60; // 1 second of continuous damage
            }
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            
            // Don't impact on buildings - pass through them!
            if (hitThing is Building)
            {
                return; // Continue traveling through buildings
            }

            // Handle pawn impacts but don't stop the projectile
            if (hitThing is Pawn hitPawn && hitPawn != launcher)
            {
                ApplyDriveImpact(hitPawn);
                return; // Don't call base.Impact, continue traveling
            }

            // Only destroy projectile when reaching max range (ticksToImpact <= 0)
            if (this.ticksToImpact <= 0)
            {
                base.Impact(hitThing, blockedByShield);
            }
        }

        private void ApplyDriveImpact(Pawn target)
        {
            if (target == null || target.Dead || target == launcher || hitPawns.Contains(target))
                return; // Don't hit null, dead, same pawn twice or the caster

            hitPawns.Add(target);

            // Calculate damage based on weapon
            var damageInfo = WeaponDamageUtility.CalculateMeleeDamage(launcher as Pawn, extraDamageMultiplier);
            if (damageInfo.HasValue)
            {
                // Apply main damage
                target.TakeDamage(damageInfo.Value);

                // Visual and sound effects with null safety
                if (target.Map != null)
                {
                    FleckMaker.Static(target.Position, target.Map, FleckDefOf.PsycastAreaEffect, 1.8f);
                    SoundStarter.PlayOneShot(SoundDefOf.Pawn_Melee_Punch_HitPawn, new TargetInfo(target.Position, target.Map));
                }

                // If this is the final projectile, apply continuous damage
                if (isFinalProjectile && continuousDamageTicks > 0)
                {
                    ApplyContinuousDamage(target);
                }
            }
        }

        private void ApplyContinuousDamage(Pawn target)
        {
            // Add a hediff that deals damage over time
            Hediff_DriveBurn burnHediff = (Hediff_DriveBurn)HediffMaker.MakeHediff(DMC_HediffDefOf.DMC_DriveBurn, target);
            burnHediff.InitializeBurn(launcher as Pawn, 60, extraDamageMultiplier * 0.3f); // 1 second, 30% of hit damage per tick
            target.health.AddHediff(burnHediff);

            // Extra visual effect for continuous damage
            FleckMaker.Static(target.Position, target.Map, FleckDefOf.PsycastAreaEffect, 2.5f);
        }

        protected override void Tick()
        {
            base.Tick();

            // Null safety checks
            if (base.Map == null || !this.Position.IsValid)
                return;

            // Create trailing visual effects while traveling
            if (this.ticksToImpact % 3 == 0) // Every 3 ticks
            {
                Vector3 drawPos = this.DrawPos;
                FleckMaker.ThrowDustPuff(drawPos, base.Map, 0.8f);
                
                // Extra effects for final projectile
                if (isFinalProjectile && base.Map != null)
                {
                    FleckMaker.Static(this.Position, base.Map, FleckDefOf.PsycastAreaEffect, 1.2f);
                }
            }

            // Check for pawns in current position (for pass-through damage)
            // Copy the list to avoid collection modification issues
            List<Thing> thingsInCell = base.Map.thingGrid.ThingsListAtFast(this.Position);
            List<Thing> thingsCopy = new List<Thing>(thingsInCell);
            
            foreach (Thing thing in thingsCopy)
            {
                if (thing is Pawn pawn && pawn != launcher && !hitPawns.Contains(pawn))
                {
                    ApplyDriveImpact(pawn);
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            // Draw projectile with enhanced visuals for final projectile
            float scale = isFinalProjectile ? 1.5f : 1f;
            Color color = isFinalProjectile ? Color.red : Color.white;
            
            // Use default drawing but with modifications
            Graphics.DrawMesh(MeshPool.plane10, 
                Matrix4x4.TRS(drawLoc, this.ExactRotation, Vector3.one * scale), 
                this.def.DrawMatSingle, 0);
        }
    }

    // Continuous damage hediff for final projectile
    public class Hediff_DriveBurn : HediffWithComps
    {
        private Pawn damageSource;
        private int remainingTicks;
        private float damagePerTick;

        public void InitializeBurn(Pawn source, int duration, float damage)
        {
            damageSource = source;
            remainingTicks = duration;
            damagePerTick = damage;
        }

        public override void Tick()
        {
            base.Tick();

            if (remainingTicks <= 0)
            {
                this.pawn.health.RemoveHediff(this);
                return;
            }

            remainingTicks--;

            // Apply damage every 10 ticks (avoid spam)
            if (remainingTicks % 10 == 0 && damageSource != null)
            {
                var damageInfo = WeaponDamageUtility.CalculateMeleeDamage(damageSource, damagePerTick);
                if (damageInfo.HasValue)
                {
                    this.pawn.TakeDamage(damageInfo.Value);
                    
                    // Small visual effect
                    FleckMaker.ThrowDustPuff(this.pawn.Position.ToVector3Shifted(), this.pawn.Map, 0.5f);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref damageSource, "damageSource");
            Scribe_Values.Look(ref remainingTicks, "remainingTicks");
            Scribe_Values.Look(ref damagePerTick, "damagePerTick");
        }
    }
}