using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DMCAbilities
{
    public class Projectile_RedOrb : Projectile
    {
        private Pawn originalCaster;
        private IntVec3 finalTarget;
        private float orbDamage;
        private int fallTicks = 0;
        private const int FallDuration = 60; // 1 second fall time
        private bool hasExploded = false;
        
        public void Initialize(Pawn caster, IntVec3 target, float damage, int delay = 0)
        {
            originalCaster = caster;
            finalTarget = target;
            orbDamage = damage;
            fallTicks = -delay; // Start with negative ticks for delay
        }
        
        // Override all movement-related methods to prevent any position changes
        protected override void TickInterval(int interval)
        {
            // Do NOT call base.TickInterval() - completely prevent projectile movement
            // Just call our custom Tick() logic
            for (int i = 0; i < interval; i++)
            {
                Tick();
            }
        }
        
        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            // Override launch to prevent any default projectile behavior
            // Don't call base.Launch() - just set up our custom behavior
            this.launcher = launcher;
            this.origin = origin;
            this.usedTarget = usedTarget;
            this.intendedTarget = intendedTarget;
            
            // Set position immediately to final target
            if (finalTarget.IsValid && base.Map != null && finalTarget.InBounds(base.Map))
            {
                base.Position = finalTarget;
            }
        }

        public void InitializeForFalling(Pawn caster, IntVec3 target, float damage, int delay = 0)
        {
            originalCaster = caster;
            finalTarget = target;
            orbDamage = damage;
            fallTicks = -delay; // Start with negative ticks for delay
            hasExploded = false;
            
            // Validate position
            if (!target.IsValid || !target.InBounds(caster?.Map))
            {
                Log.Error($"Red Hot Night Orb: Invalid target position {target}!");
                finalTarget = caster?.Position ?? IntVec3.Zero;
            }
            
            // Create STRONG initial spawn effect to show orb appearing
            if (base.Map != null && finalTarget.InBounds(base.Map))
            {
                // Multiple flashes for visibility
                FleckMaker.ThrowExplosionCell(finalTarget, base.Map, FleckDefOf.ExplosionFlash, Color.yellow);
                FleckMaker.ThrowExplosionCell(finalTarget, base.Map, FleckDefOf.ExplosionFlash, Color.red);
                FleckMaker.ThrowFireGlow(finalTarget.ToVector3Shifted(), base.Map, 3f);
                FleckMaker.ThrowHeatGlow(finalTarget, base.Map, 2f);
                
                // Add targeting indicator
                FleckMaker.Static(finalTarget, base.Map, FleckDefOf.PsycastAreaEffect, 1.5f);
            }
        }

        protected override void Tick()
        {
            // Comprehensive safety checks first
            if (hasExploded || this.Destroyed) return;
            
            if (base.Map == null)
            {
                Log.Warning("Red Hot Night Orb: Map is null, destroying");
                this.Destroy(DestroyMode.Vanish);
                return;
            }
            
            if (originalCaster == null)
            {
                Log.Warning("Red Hot Night Orb: Original caster is null, destroying");
                this.Destroy(DestroyMode.Vanish);
                return;
            }
            
            // Validate target position
            if (!finalTarget.IsValid || !finalTarget.InBounds(base.Map))
            {
                finalTarget = Position;
            }
            
            // COMPLETELY prevent projectile movement - don't call base.Tick()
            // Force position to stay at target
            if (Position != finalTarget && finalTarget.InBounds(base.Map))
            {
                try 
                {
                    base.Position = finalTarget;
                }
                catch (System.Exception e)
                {
                    Log.Error($"Red Hot Night Orb: Failed to set position: {e}");
                    this.Destroy(DestroyMode.Vanish);
                    return;
                }
            }
            
            fallTicks++;
            
            // Don't start effects until delay is over
            if (fallTicks <= 0) return;
            
            // Create VERY visible falling effects - enhanced visibility
            if (fallTicks > 0 && base.Map != null)
            {
                Vector3 currentPos = this.DrawPos;
                
                // Large, bright fire effects every tick for maximum visibility
                if (fallTicks % 2 == 0)
                {
                    FleckMaker.ThrowFireGlow(currentPos, base.Map, 3f); // Increased size
                    FleckMaker.ThrowHeatGlow(this.Position, base.Map, 2f);
                }
                
                // Explosion flash effect to create "orb" visual
                if (fallTicks % 4 == 0)
                {
                    FleckMaker.ThrowExplosionCell(this.Position, base.Map, FleckDefOf.ExplosionFlash, Color.red);
                }
                
                // Dust and smoke for falling effect
                if (fallTicks % 3 == 0)
                {
                    FleckMaker.ThrowDustPuff(currentPos, base.Map, 1.2f);
                }
                
                // Micro sparks for magical effect
                if (fallTicks % 2 == 0)
                {
                    FleckMaker.ThrowMicroSparks(currentPos, base.Map);
                }
                
                // Heat shimmer for dramatic effect
                if (fallTicks % 8 == 0)
                {
                    FleckMaker.ThrowHeatGlow(this.Position, base.Map, 1.2f);
                }
                
                // Sparks effect to show orb is "charging"
                if (fallTicks % 6 == 0)
                {
                    FleckMaker.ThrowMicroSparks(currentPos, base.Map);
                }
                
                // Growing intensity as it gets closer to explosion
                if (fallTicks > FallDuration - 20 && fallTicks % 2 == 0)
                {
                    FleckMaker.ThrowFireGlow(currentPos, base.Map, 2f);
                }
            }
            
            // Impact after fall duration
            if (fallTicks >= FallDuration)
            {
                ExplodeOnImpact();
            }
        }

        private void ExplodeOnImpact()
        {
            if (hasExploded || base.Map == null) return;
            
            // Validate explosion position
            if (!finalTarget.IsValid || !finalTarget.InBounds(base.Map))
            {
                Log.Warning($"Red Hot Night Orb: Invalid explosion target {finalTarget}, using current position {Position}");
                finalTarget = Position;
            }
            
            hasExploded = true;
            
            // Explosion sound and visual
            SoundStarter.PlayOneShot(SoundDefOf.Thunder_OnMap, new TargetInfo(finalTarget, base.Map));
            FleckMaker.ThrowExplosionCell(finalTarget, base.Map, FleckDefOf.ExplosionFlash, Color.red);
            
            // Apply custom explosion damage with friendly fire protection
            ApplyExplosionDamage();
            
            // Create visual explosion effects only (no damage)
            GenExplosion.DoExplosion(
                center: finalTarget,
                map: base.Map,
                radius: 2.5f,
                damType: DamageDefOf.Burn,
                instigator: originalCaster,
                damAmount: 0, // No damage - we handle it separately
                armorPenetration: 0f,
                explosionSound: null, // Already played above
                weapon: null, // NULL weapon = shows ability name
                projectile: this.def,
                intendedTarget: null,
                postExplosionSpawnThingDef: null,
                postExplosionSpawnChance: 0f,
                postExplosionSpawnThingCount: 1,
                preExplosionSpawnThingDef: null,
                preExplosionSpawnChance: 0f,
                preExplosionSpawnThingCount: 1,
                chanceToStartFire: 0.6f,
                damageFalloff: true,
                direction: null,
                ignoredThings: null
            );
            
            // Apply Red Hot Burn debuff to affected pawns
            ApplyBurnDebuff();
            
            // Extra DMC-style visual effects
            CreateDMCVisualEffects();
            
            // Clean up
            this.Destroy(DestroyMode.Vanish);
        }
        
        private void ApplyExplosionDamage()
        {
            float explosionRadius = 2.5f;
            
            // Find all pawns in explosion radius
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(finalTarget, explosionRadius, true))
            {
                if (!cell.InBounds(base.Map)) continue;
                
                // Copy the list to avoid collection modification during enumeration
                List<Thing> originalThings = base.Map.thingGrid.ThingsListAtFast(cell);
                List<Thing> things = new List<Thing>(originalThings);
                foreach (Thing thing in things)
                {
                    if (!(thing is Pawn pawn) || pawn.Dead || pawn.Destroyed) continue;
                    
                    // Apply friendly fire protection
                    if (DMCAbilitiesMod.settings?.disableFriendlyFire == true && 
                        !WeaponDamageUtility.ShouldTargetPawn(originalCaster, pawn))
                    {
                        continue; // Skip friendly targets
                    }
                    
                    // Calculate damage based on distance (damage falloff)
                    float distance = pawn.Position.DistanceTo(finalTarget);
                    float damageMultiplier = Mathf.Lerp(1f, 0.3f, distance / explosionRadius);
                    int finalDamage = Mathf.RoundToInt(orbDamage * damageMultiplier);
                    
                    if (finalDamage > 0)
                    {
                        // Create damage info with proper attribution
                        DamageInfo damageInfo = new DamageInfo(
                            def: DamageDefOf.Burn,
                            amount: finalDamage,
                            armorPenetration: 0.15f,
                            angle: 0f,
                            instigator: originalCaster,
                            weapon: null // Shows ability name
                        );
                        
                        pawn.TakeDamage(damageInfo);
                    }
                }
            }
        }
        
        private void ApplyBurnDebuff()
        {
            float radius = 3.5f; // Fixed radius instead of relying on def.projectile which might be null
            
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(finalTarget, radius, true))
            {
                if (!cell.InBounds(base.Map)) continue;
                
                // Copy the list to avoid collection modification during enumeration
                List<Thing> originalThings = base.Map.thingGrid.ThingsListAtFast(cell);
                List<Thing> things = new List<Thing>(originalThings);
                foreach (Thing thing in things)
                {
                    if (!(thing is Pawn pawn) || pawn.Dead || pawn.Destroyed) continue;
                    
                    // Check friendly fire settings
                    if (DMCAbilitiesMod.settings?.disableFriendlyFire == true && 
                        !WeaponDamageUtility.ShouldTargetPawn(originalCaster, pawn))
                    {
                        continue;
                    }
                    
                    // Apply burn debuff with safe casting
                    float distance = pawn.Position.DistanceTo(finalTarget);
                    float burnSeverity = Mathf.Lerp(2f, 0.5f, distance / radius); // Closer = more severe
                    
                    try
                    {
                        Hediff burnHediff = HediffMaker.MakeHediff(DMC_HediffDefOf.DMC_RedHotBurn, pawn);
                        if (burnHediff is Hediff_RedHotBurn redHotBurn)
                        {
                            redHotBurn.InitializeBurn(originalCaster, Mathf.RoundToInt(120 * burnSeverity), orbDamage * 0.1f);
                        }
                        pawn.health.AddHediff(burnHediff);
                    }
                    catch (System.Exception e)
                    {
                        Log.Warning($"Red Hot Night: Failed to apply burn debuff: {e}");
                    }
                }
            }
        }
        
        private void CreateDMCVisualEffects()
        {
            // Large explosion flash
            for (int i = 0; i < 5; i++)
            {
                Vector2 circleOffset = Rand.InsideUnitCircle * 2f;
                Vector3 flashPos = finalTarget.ToVector3() + new Vector3(circleOffset.x, 0, circleOffset.y);
                FleckMaker.ThrowExplosionCell(flashPos.ToIntVec3(), base.Map, FleckDefOf.ExplosionFlash, Color.red);
            }
            
            // Fire and heat effects
            for (int i = 0; i < 8; i++)
            {
                Vector2 circleOffset = Rand.InsideUnitCircle * 3f;
                Vector3 firePos = finalTarget.ToVector3() + new Vector3(circleOffset.x, 0, circleOffset.y);
                FleckMaker.ThrowFireGlow(firePos, base.Map, 2f);
                FleckMaker.ThrowHeatGlow(firePos.ToIntVec3(), base.Map, 1.5f);
            }
            
            // Dust and debris
            for (int i = 0; i < 12; i++)
            {
                Vector2 circleOffset = Rand.InsideUnitCircle * 4f;
                Vector3 dustPos = finalTarget.ToVector3() + new Vector3(circleOffset.x, 0, circleOffset.y);
                FleckMaker.ThrowDustPuff(dustPos, base.Map, 1.5f);
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            // Draw orb with pulsing red glow effect
            float pulseScale = 1f + (Mathf.Sin(Find.TickManager.TicksGame * 0.1f) * 0.2f);
            Color orbColor = Color.Lerp(Color.red, Color.yellow, Mathf.PingPong(Find.TickManager.TicksGame * 0.05f, 1f));
            
            // Draw the orb with custom scaling and color
            Matrix4x4 matrix = Matrix4x4.TRS(drawLoc, this.ExactRotation, Vector3.one * pulseScale * 1.5f);
            Graphics.DrawMesh(MeshPool.plane10, matrix, def.DrawMatSingle, 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref originalCaster, "originalCaster");
            Scribe_Values.Look(ref finalTarget, "finalTarget");
            Scribe_Values.Look(ref orbDamage, "orbDamage");
            Scribe_Values.Look(ref fallTicks, "fallTicks");
        }
    }

    // Red Hot Burn hediff with damage over time
    public class Hediff_RedHotBurn : HediffWithComps
    {
        private Pawn burnSource;
        private int remainingTicks;
        private float damagePerTick;

        public void InitializeBurn(Pawn source, int duration, float damage)
        {
            burnSource = source;
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

            // Apply damage every 20 ticks (1/3 second)
            if (remainingTicks % 20 == 0 && burnSource != null && damagePerTick > 0)
            {
                // Create damage info
                DamageInfo burnDamage = new DamageInfo(
                    def: DamageDefOf.Burn,
                    amount: Mathf.RoundToInt(damagePerTick),
                    armorPenetration: 0f,
                    angle: 0f,
                    instigator: burnSource,
                    hitPart: null,
                    weapon: null // NULL weapon = shows Red Hot Night instead of weapon
                );

                this.pawn.TakeDamage(burnDamage);
                
                // Visual effect for burn damage
                if (this.pawn.Map != null)
                {
                    FleckMaker.ThrowMicroSparks(this.pawn.Position.ToVector3Shifted(), this.pawn.Map);
                    
                    // Occasional larger effect
                    if (Rand.Chance(0.3f))
                    {
                        FleckMaker.ThrowFireGlow(this.pawn.Position.ToVector3Shifted(), this.pawn.Map, 0.5f);
                    }
                }
            }
        }

        public override string TipStringExtra
        {
            get
            {
                float remainingSeconds = (float)remainingTicks / 60f;
                return $"Remaining: {remainingSeconds:F1}s\nDamage per burn: {damagePerTick:F1}";
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref burnSource, "burnSource");
            Scribe_Values.Look(ref remainingTicks, "remainingTicks");
            Scribe_Values.Look(ref damagePerTick, "damagePerTick");
        }
    }
}