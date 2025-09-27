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

            // Start dash-to-target job instead of instant teleport
            var job = JobMaker.MakeJob(DMC_JobDefOf.DMC_GunStingerCast, target);
            job.verbToUse = this;
            caster.jobs.TryTakeOrderedJob(job, JobTag.Misc);

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
                // Apply friendly fire protection
                if (target is Pawn targetPawn && DMCAbilitiesMod.settings?.disableFriendlyFire == true && 
                    !WeaponDamageUtility.ShouldTargetPawn(caster, targetPawn))
                {
                    return; // Skip friendly targets
                }
                
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
            // Null safety checks
            if (caster == null || primaryTarget == null || primaryTarget.Map == null)
                return;

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
                // Additional null safety for each pawn
                if (pawn == null || pawn.Dead || pawn.Map == null)
                    continue;

                // Calculate distance falloff (closer = more damage)
                float distance = targetPos.DistanceTo(pawn.Position);
                float falloffMultiplier = Mathf.Lerp(0.7f, 0.3f, distance / maxRange); // 70% damage at close range, 30% at max range

                // Create reduced damage info with null safety for weapon
                // Apply friendly fire protection for blast damage
                if (DMCAbilitiesMod.settings?.disableFriendlyFire == true && 
                    !WeaponDamageUtility.ShouldTargetPawn(caster, pawn))
                {
                    continue; // Skip friendly targets
                }
                
                DamageInfo blastDamage = new DamageInfo(
                    def: originalDamage.Def,
                    amount: (int)(originalDamage.Amount * falloffMultiplier),
                    armorPenetration: 0.15f, // Reduced armor penetration for blast damage
                    angle: originalDamage.Angle,
                    instigator: caster,
                    weapon: originalDamage.Weapon // This can be null, DamageInfo handles it
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
                
                // Draw radius ring around blast area for clarity
                GenDraw.DrawRadiusRing(targetPos, 3f);
                
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

    public class JobDriver_GunStingerCast : JobDriver
    {
        private List<IntVec3> dashPath;
        private int currentPathIndex = 0;
        private const int TicksBetweenCells = 1; // Fast dash for Gun Stinger
        private int lastMoveTick = 0;
        private bool hasShot = false;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);

            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);

            var gunStingerDashToil = new Toil();
            gunStingerDashToil.initAction = () =>
            {
                // Calculate direct dash path to target
                CalculateGunStingerDashPath();
                currentPathIndex = 0;
                lastMoveTick = Find.TickManager.TicksGame;
                hasShot = false;

                // Face the target
                pawn.rotationTracker.FaceTarget(TargetA);

                // Starting dash to target
            };

            gunStingerDashToil.tickAction = () =>
            {
                if (pawn.Dead || pawn.Downed)
                {
                    EndJobWith(JobCondition.Errored);
                    return;
                }

                // Check if dash is complete
                if (currentPathIndex >= dashPath.Count)
                {
                    // Perform final shotgun blast if we haven't already
                    if (!hasShot && TargetA.HasThing && TargetA.Thing is Pawn target)
                    {
                        PerformGunStingerShot(target);
                    }
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }

                // Dash forward rapidly
                if (Find.TickManager.TicksGame - lastMoveTick >= TicksBetweenCells)
                {
                    IntVec3 nextCell = dashPath[currentPathIndex];
                    
                    // Force dash through obstacles (trees, walls, etc.)
                    WeaponDamageUtility.ForceTeleportPawn(pawn, nextCell);
                    // Dashing through obstacles
                    
                    // Check if we're close enough for the point-blank shot
                    if (TargetA.HasThing && TargetA.Thing is Pawn target && 
                        pawn.Position.AdjacentTo8WayOrInside(target.Position) && !hasShot)
                    {
                        PerformGunStingerShot(target);
                        hasShot = true;
                    }
                    
                    currentPathIndex++;
                    lastMoveTick = Find.TickManager.TicksGame;
                }
            };

            gunStingerDashToil.defaultCompleteMode = ToilCompleteMode.Never;
            yield return gunStingerDashToil;
        }

        private void CalculateGunStingerDashPath()
        {
            dashPath = new List<IntVec3>();
            
            IntVec3 startPos = pawn.Position;
            IntVec3 targetPos = TargetA.Cell;
            
            // Create direct path to target
            List<IntVec3> lineCells = GenSight.PointsOnLineOfSight(startPos, targetPos).ToList();
            
            // Remove starting position
            if (lineCells.Count > 0 && lineCells[0] == startPos)
                lineCells.RemoveAt(0);

            // Add all cells up to (but NOT including) the target position - bypass obstacles
            // This ensures we land adjacent to the enemy, not on top (like regular Stinger)
            foreach (IntVec3 cell in lineCells)
            {
                if (cell == targetPos)
                {
                    // Stop before reaching the target position - we want to be adjacent
                    break;
                }
                
                if (cell.InBounds(pawn.Map))
                {
                    // Add ALL cells - Gun Stinger bypasses trees, walls, everything
                    dashPath.Add(cell);
                }
            }

            // If the path is empty (target too close), find an adjacent position
            if (dashPath.Count == 0)
            {
                IntVec3 adjacentPos = WeaponDamageUtility.FindSafeTeleportPosition(targetPos, pawn.Map, pawn, 1);
                if (adjacentPos != IntVec3.Invalid && adjacentPos != startPos)
                {
                    dashPath.Add(adjacentPos);
                }
            }

            // Dash path calculated
        }

        private void PerformGunStingerShot(Pawn target)
        {
            if (target == null || target.Dead) return;

            // Calculate enhanced shotgun damage for point-blank shot
            float multiplier = DMCAbilitiesMod.settings?.gunStingerDamageMultiplier ?? 1.5f;
            var damageInfo = WeaponDamageUtility.CalculateRangedDamage(pawn, multiplier);
            
            if (damageInfo.HasValue)
            {
                // Apply friendly fire protection
                if (DMCAbilitiesMod.settings?.disableFriendlyFire == true && 
                    !WeaponDamageUtility.ShouldTargetPawn(pawn, target))
                {
                    return; // Skip friendly targets
                }
                
                target.TakeDamage(damageInfo.Value);
                
                // Apply stronger stagger from shotgun blast
                if (target.health?.hediffSet != null)
                {
                    Hediff stagger = HediffMaker.MakeHediff(DMC_HediffDefOf.DMC_Stagger, target);
                    stagger.Severity = 0.6f; // Very strong stagger from shotgun blast
                    target.health.AddHediff(stagger);
                }

                // Create shotgun blast effects
                CreateGunStingerBlastEffects(target.Position);
                
                // Shotgun blast hit target
            }

            // NEW: Perform 90-degree cone area blast after point-blank shot
            PerformConeAreaBlast(target.Position);
        }

        private void CreateGunStingerBlastEffects(IntVec3 position)
        {
            Map map = pawn.Map;
            if (map == null) return;

            // Shotgun blast effect
            FleckMaker.Static(position, map, FleckDefOf.ExplosionFlash, 1.5f);
            
            // Shotgun sound
            SoundStarter.PlayOneShot(SoundDefOf.Pawn_Melee_Punch_HitPawn, new TargetInfo(position, map));
        }

        private void PerformConeAreaBlast(IntVec3 centerPosition)
        {
            Map map = pawn.Map;
            if (map == null) return;

            // Get the facing direction of the caster
            Rot4 facingDirection = pawn.Rotation;
            
            // Get cells in a 90-degree cone blast area
            List<IntVec3> coneCells = WeaponDamageUtility.GetConeBlastCells(centerPosition, facingDirection, map, 3);
            
            // Performing cone blast

            // Apply damage to all entities in the cone area
            foreach (IntVec3 cell in coneCells)
            {
                // Get all things at this cell
                List<Thing> thingsInCell = map.thingGrid.ThingsListAtFast(cell).ToList();
                
                foreach (Thing thing in thingsInCell)
                {
                    // Target pawns (animals, mechs, humanoids) and turrets, but not other buildings
                    if ((thing is Pawn targetPawn && targetPawn != pawn && !targetPawn.Dead) ||
                        (thing.def.building?.IsTurret == true))
                    {
                        // Apply reduced cone damage (0.8x normal damage)
                        float coneMultiplier = (DMCAbilitiesMod.settings?.gunStingerDamageMultiplier ?? 1.5f) * 0.8f;
                        var damageInfo = WeaponDamageUtility.CalculateRangedDamage(pawn, coneMultiplier);
                        
                        if (damageInfo.HasValue)
                        {
                            // Apply friendly fire protection for cone damage
                            if (thing is Pawn coneTarget && DMCAbilitiesMod.settings?.disableFriendlyFire == true && 
                                !WeaponDamageUtility.ShouldTargetPawn(pawn, coneTarget))
                            {
                                continue; // Skip friendly targets
                            }
                            
                            thing.TakeDamage(damageInfo.Value);
                            
                            // Apply light stagger only to pawns (turrets don't have health hediffs)
                            if (thing is Pawn pawnTarget && pawnTarget.health?.hediffSet != null)
                            {
                                Hediff stagger = HediffMaker.MakeHediff(DMC_HediffDefOf.DMC_Stagger, pawnTarget);
                                stagger.Severity = 0.3f; // Lighter stagger for area effect
                                pawnTarget.health.AddHediff(stagger);
                            }
                            
                            // Cone blast hit target
                        }
                    }
                }
                
                // Create blast effects at each cell
                FleckMaker.Static(cell, map, FleckDefOf.ExplosionFlash, 0.8f);
            }
            
            // Play area blast sound
            SoundStarter.PlayOneShot(SoundDefOf.Pawn_Melee_Punch_HitPawn, new TargetInfo(centerPosition, map));
        }
    }
}