using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace DMCAbilities
{
    public class Verb_RapidSlash : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            // Check if mod and ability are enabled
            if (DMCAbilitiesMod.settings != null && 
                (!DMCAbilitiesMod.settings.modEnabled || !DMCAbilitiesMod.settings.rapidSlashEnabled))
            {
                return false;
            }

            // Check for melee weapon requirement
            if (!WeaponDamageUtility.HasMeleeWeapon(CasterPawn))
            {
                Messages.Message(WeaponDamageUtility.GetNoMeleeWeaponMessage(), 
                    CasterPawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // Create dash job - can target terrain or pawns
            var job = JobMaker.MakeJob(DMC_JobDefOf.DMC_RapidSlashCast, CurrentTarget);
            job.verbToUse = this;
            CasterPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            return true;
        }

        // Show the dash path and 3x3 damage area
        public override void DrawHighlight(LocalTargetInfo target)
        {
            base.DrawHighlight(target);
            
            if (target.IsValid && CasterPawn != null)
            {
                // Draw dash path from caster to target (straight line)
                List<IntVec3> pathCells = GenSight.PointsOnLineOfSight(CasterPawn.Position, target.Cell).ToList();
                GenDraw.DrawFieldEdges(pathCells, Color.yellow);
                
                // Draw 3x3 damage areas along the path
                foreach (IntVec3 cell in pathCells)
                {
                    List<IntVec3> damageArea = GenAdj.CellsAdjacent8Way(new TargetInfo(cell, CasterPawn.Map)).ToList();
                    damageArea.Add(cell); // Include center cell
                    GenDraw.DrawFieldEdges(damageArea, Color.red);
                }
            }
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = false;
            return verbProps.range;
        }
    }

    public class JobDriver_RapidSlashCast : JobDriver
    {
        private List<IntVec3> dashPath;
        private int currentPathIndex = 0;
        private const int TicksBetweenCells = 2; // Very fast forward dash
        private int lastMoveTick = 0;
        private HashSet<Thing> alreadyHit = new HashSet<Thing>();

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);

            var forwardDashToil = new Toil();
            forwardDashToil.initAction = () =>
            {
                // Calculate forward dash path - can target terrain or pawns
                CalculateForwardDashPath();
                currentPathIndex = 0;
                lastMoveTick = Find.TickManager.TicksGame;
                alreadyHit.Clear();

                // Face the dash direction
                pawn.rotationTracker.FaceCell(TargetA.Cell);

                Log.Message($"Rapid Slash: Starting forward dash through {dashPath.Count} cells to {TargetA.Cell}");
            };

            forwardDashToil.tickAction = () =>
            {
                if (pawn.Dead || pawn.Downed)
                {
                    EndJobWith(JobCondition.Errored);
                    return;
                }

                // Check if dash is complete
                if (currentPathIndex >= dashPath.Count)
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }

                // Move forward through dash path rapidly
                if (Find.TickManager.TicksGame - lastMoveTick >= TicksBetweenCells)
                {
                    IntVec3 nextCell = dashPath[currentPathIndex];
                    
                    // Force dash through ALL obstacles (trees, walls, etc.)
                    WeaponDamageUtility.ForceTeleportPawn(pawn, nextCell);
                    Log.Message($"Rapid Slash: Bypassing obstacles, dashing to {nextCell} ({currentPathIndex + 1}/{dashPath.Count})");
                    
                    // Slash everything in 3x3 area around current position
                    SlashInThreeByThreeArea(nextCell);
                    
                    currentPathIndex++;
                    lastMoveTick = Find.TickManager.TicksGame;
                }
            };

            forwardDashToil.defaultCompleteMode = ToilCompleteMode.Never;
            yield return forwardDashToil;
        }

        private void CalculateForwardDashPath()
        {
            dashPath = new List<IntVec3>();
            
            IntVec3 startPos = pawn.Position;
            IntVec3 targetPos = TargetA.Cell;
            
            // Create straight line path from start to target (forward dash)
            List<IntVec3> lineCells = GenSight.PointsOnLineOfSight(startPos, targetPos).ToList();
            
            // Remove starting position (we're already there)
            if (lineCells.Count > 0 && lineCells[0] == startPos)
                lineCells.RemoveAt(0);

            // Uncapped dash distance based on melee skill
            int meleeSkill = pawn?.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? 0;
            int maxDashCells = 10 + (meleeSkill / 2); // 10 base + 0.5 per melee level (uncapped)

            // Take up to max dash cells along the path to target - bypass ALL obstacles
            for (int i = 0; i < Mathf.Min(lineCells.Count, maxDashCells); i++)
            {
                IntVec3 cell = lineCells[i];
                if (cell.InBounds(pawn.Map))
                {
                    // Add ALL cells in path - Rapid Slash bypasses trees, walls, everything
                    dashPath.Add(cell);
                }
            }

            Log.Message($"Rapid Slash: Forward dash path calculated with {dashPath.Count} cells (melee skill: {meleeSkill}, max range: {maxDashCells})");
        }

        private void SlashInThreeByThreeArea(IntVec3 centerCell)
        {
            Map map = pawn.Map;
            if (map == null) return;

            // Create 3x3 area around current position (user requested 3x3 instead of 1x1)
            List<IntVec3> threeBythreeCells = new List<IntVec3>();
            
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    IntVec3 cell = centerCell + new IntVec3(x, 0, z);
                    if (cell.InBounds(map))
                    {
                        threeBythreeCells.Add(cell);
                    }
                }
            }
            
            foreach (IntVec3 cell in threeBythreeCells)
            {
                // Find pawns in this cell - CRITICAL: Use .ToList() to prevent collection modification errors
                List<Thing> things = map.thingGrid.ThingsListAtFast(cell).ToList();
                foreach (Thing thing in things)
                {
                    // Target pawns (animals, mechs, humanoids) and turrets, but not other buildings
                    if (((thing is Pawn targetPawn && targetPawn != pawn && !targetPawn.Dead) ||
                         (thing.def.building?.IsTurret == true)) &&
                        !alreadyHit.Contains(thing))
                    {
                        // Rapid Slash damages ANY pawn, mecha, turret, or animal in the path
                        PerformDashSlash(thing);
                        alreadyHit.Add(thing); // Each target can only be hit once during the dash
                    }
                }
            }
        }

        private void PerformDashSlash(Thing target)
        {
            if (target == null || target.Destroyed) return;

            // Calculate and apply melee damage for forward dash slashes
            float damageMultiplier = 1.0f; // Standard weapon damage for each slash
            var damageInfo = WeaponDamageUtility.CalculateMeleeDamage(pawn, damageMultiplier);
            
            if (damageInfo.HasValue)
            {
                target.TakeDamage(damageInfo.Value);
            }

            // Apply stagger effect only to pawns (turrets don't have health hediffs)
            if (target is Pawn pawnTarget && pawnTarget.health?.hediffSet != null)
            {
                Hediff stagger = HediffMaker.MakeHediff(DMC_HediffDefOf.DMC_Stagger, pawnTarget);
                stagger.Severity = 0.4f; // Strong stagger from dash attack
                pawnTarget.health.AddHediff(stagger);
            }

            // Try to spawn spectral summoned sword above target
            TrySpawnSpectralSword(target);

            // Visual and sound effects
            CreateSlashEffects(target.Position);

            Log.Message($"Rapid Slash: Forward dash hit {target.Label}");
        }

        private void TrySpawnSpectralSword(Thing target)
        {
            // Calculate chance: 10% base + 1% per 5 melee skill levels
            int meleeSkill = pawn?.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? 0;
            float baseChance = 10f; // 10% base chance
            float skillBonus = (meleeSkill / 5) * 1f; // +1% per 5 skill levels
            float totalChance = (baseChance + skillBonus) / 100f;

            if (Rand.Chance(totalChance))
            {
                SpawnSpectralSword(target.Position);
                Log.Message($"Rapid Slash: Spawned spectral sword above {target.Label} (chance: {totalChance:P1})");
            }
        }

        private void SpawnSpectralSword(IntVec3 targetPosition)
        {
            Map map = pawn.Map;
            if (map == null) return;

            // Spawn spectral sword projectile on same cell (visually above their head)
            
            Thing projectileThing = ThingMaker.MakeThing(DMC_ThingDefOf.DMC_SpectralSwordRapidSlashProjectile);
            Projectile_SpectralSwordRapidSlash projectile = projectileThing as Projectile_SpectralSwordRapidSlash;
            
            if (projectile != null)
            {
                // Set explosion delay and target
                projectile.SetExplosionTarget(targetPosition, pawn);
                
                // Spawn on the same cell as the target (visually appears above their head)
                GenSpawn.Spawn(projectile, targetPosition, map);
                projectile.Launch(pawn, targetPosition, targetPosition, ProjectileHitFlags.IntendedTarget);
                
                // Visual effect at spawn (on same cell, visually above)
                FleckMaker.Static(targetPosition, map, FleckDefOf.PsycastAreaEffect, 1.0f);
            }
        }

        private void CreateSlashEffects(IntVec3 position)
        {
            Map map = pawn.Map;
            if (map == null) return;

            // Blood splatter effect
            FleckMaker.ThrowDustPuffThick(position.ToVector3Shifted(), map, 1.0f, UnityEngine.Color.red);
            
            // Slash sparks
            FleckMaker.Static(position, map, FleckDefOf.MicroSparks, 1.2f);
            
            // Dramatic blade slash sound for dash attack
            SoundStarter.PlayOneShot(SoundDefOf.Psycast_Skip_Entry, new TargetInfo(position, map));
        }
    }
}