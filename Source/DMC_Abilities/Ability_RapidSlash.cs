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
        private HashSet<Pawn> alreadyHit = new HashSet<Pawn>();

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
                    
                    // Move to next cell (dash forward)
                    if (WeaponDamageUtility.SafeTeleportPawn(pawn, nextCell))
                    {
                        Log.Message($"Rapid Slash: Dashing forward to {nextCell} ({currentPathIndex + 1}/{dashPath.Count})");
                        
                        // Slash everything in 3x3 area around current position
                        SlashInThreeByThreeArea(nextCell);
                        
                        currentPathIndex++;
                        lastMoveTick = Find.TickManager.TicksGame;
                    }
                    else
                    {
                        // Hit obstacle, end dash
                        Log.Warning($"Rapid Slash: Hit obstacle at {nextCell}, ending dash");
                        EndJobWith(JobCondition.Succeeded);
                        return;
                    }
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

            // Increased dash distance based on melee skill (user requested increased range)
            int meleeSkill = pawn?.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? 0;
            int maxDashCells = 10 + (meleeSkill / 2); // Increased from 6 to 10 base range
            maxDashCells = Mathf.Clamp(maxDashCells, 10, 20); // Max range increased to 20

            // Take up to max dash cells along the path to target
            for (int i = 0; i < Mathf.Min(lineCells.Count, maxDashCells); i++)
            {
                IntVec3 cell = lineCells[i];
                if (cell.InBounds(pawn.Map) && cell.Standable(pawn.Map))
                {
                    dashPath.Add(cell);
                }
                else
                {
                    // Stop at first impassable cell
                    break;
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
                // Find pawns in this cell
                List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
                foreach (Thing thing in things)
                {
                    Pawn targetPawn = thing as Pawn;
                    if (targetPawn != null && 
                        targetPawn != pawn && 
                        !targetPawn.Dead && 
                        !alreadyHit.Contains(targetPawn))
                    {
                        // Rapid Slash damages ANY pawn, mecha, or animal in the path
                        PerformDashSlash(targetPawn);
                        alreadyHit.Add(targetPawn); // Each target can only be hit once during the dash
                    }
                }
            }
        }

        private void PerformDashSlash(Pawn target)
        {
            if (target == null || target.Dead) return;

            // Calculate and apply melee damage for forward dash slashes
            float damageMultiplier = 1.0f; // Standard weapon damage for each slash
            var damageInfo = WeaponDamageUtility.CalculateMeleeDamage(pawn, damageMultiplier);
            
            if (damageInfo.HasValue)
            {
                target.TakeDamage(damageInfo.Value);
            }

            // Apply stagger effect
            if (target.health?.hediffSet != null)
            {
                Hediff stagger = HediffMaker.MakeHediff(DMC_HediffDefOf.DMC_Stagger, target);
                stagger.Severity = 0.4f; // Strong stagger from dash attack
                target.health.AddHediff(stagger);
            }

            // Visual and sound effects
            CreateSlashEffects(target.Position);

            Log.Message($"Rapid Slash: Forward dash hit {target.Label}");
        }

        private void CreateSlashEffects(IntVec3 position)
        {
            Map map = pawn.Map;
            if (map == null) return;

            // Blood splatter effect
            FleckMaker.ThrowDustPuffThick(position.ToVector3Shifted(), map, 1.0f, UnityEngine.Color.red);
            
            // Slash sparks
            FleckMaker.Static(position, map, FleckDefOf.MicroSparks, 1.2f);
            
            // Blade slash sound for dash attack
            SoundStarter.PlayOneShot(SoundDefOf.Pawn_Melee_Punch_HitPawn, new TargetInfo(position, map));
        }
    }
}