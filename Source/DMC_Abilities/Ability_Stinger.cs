using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace DMCAbilities
{
    public class Verb_Stinger : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            // Check if mod and ability are enabled
            if (DMCAbilitiesMod.settings != null && 
                (!DMCAbilitiesMod.settings.modEnabled || !DMCAbilitiesMod.settings.stingerEnabled))
            {
                return false;
            }

            if (currentTarget.HasThing && currentTarget.Thing is Pawn targetPawn)
            {
                return CastStinger(targetPawn);
            }
            return false;
        }

        private bool CastStinger(Pawn target)
        {
            Pawn caster = CasterPawn;
            if (caster == null || target == null)
                return false;

            // Check for melee weapon
            if (!WeaponDamageUtility.HasMeleeWeapon(caster))
            {
                Messages.Message(WeaponDamageUtility.GetNoMeleeWeaponMessage(), 
                    caster, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // Start dash-to-target job instead of instant teleport
            var job = JobMaker.MakeJob(DMC_JobDefOf.DMC_StingerCast, target);
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

        private void CreateStingerEffects(IntVec3 originPos, IntVec3 targetPos)
        {
            Map map = CasterPawn.Map;

            // Psycast effect at dash origin
            FleckMaker.Static(originPos, map, FleckDefOf.PsycastAreaEffect, 2f);
            
            // Dramatic psycast effect at target
            FleckMaker.Static(targetPos, map, FleckDefOf.PsycastAreaEffect, 2.5f);
            
            // Play impact sound using the correct RimWorld method
            SoundStarter.PlayOneShot(SoundDefOf.Pawn_Melee_Punch_HitPawn, new TargetInfo(targetPos, map));
        }

        private void ApplyStingerDamage(Pawn caster, Pawn target)
        {
            // Null safety checks
            if (caster == null || target == null || target.Dead || target.Map == null)
                return;

            // Check if Stinger is enabled in settings
            if (DMCAbilitiesMod.settings != null && !DMCAbilitiesMod.settings.stingerEnabled)
                return;

            // Calculate damage with configurable multiplier
            float multiplier = DMCAbilitiesMod.settings?.stingerDamageMultiplier ?? 1.2f;
            var damageInfo = WeaponDamageUtility.CalculateMeleeDamage(caster, multiplier);
            if (damageInfo.HasValue)
            {
                // Apply damage
                target.TakeDamage(damageInfo.Value);

                // Create impact effect using psycast effect with null safety
                if (target.Map != null)
                {
                    FleckMaker.Static(target.Position, target.Map, FleckDefOf.PsycastAreaEffect, 1.5f);
                }
            }
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = true;
            return this.verbProps.range; // Show ability range during targeting
        }

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.ValidateTarget(target, showMessages))
                return false;

            if (!target.HasThing || !(target.Thing is Pawn targetPawn))
            {
                if (showMessages)
                    Messages.Message("DMC_MustTargetPawn".Translate(), 
                        MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (targetPawn == CasterPawn)
            {
                if (showMessages)
                    Messages.Message("DMC_CannotTargetSelf".Translate(), 
                        MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return true;
        }
    }

    public class JobDriver_StingerCast : JobDriver
    {
        private List<IntVec3> dashPath;
        private int currentPathIndex = 0;
        private const int TicksBetweenCells = 1; // Faster dash for Stinger
        private int lastMoveTick = 0;
        private bool hasStruck = false;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);

            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);

            var stingerDashToil = new Toil();
            stingerDashToil.initAction = () =>
            {
                // Calculate direct dash path to target
                CalculateStingerDashPath();
                currentPathIndex = 0;
                lastMoveTick = Find.TickManager.TicksGame;
                hasStruck = false;

                // Face the target
                pawn.rotationTracker.FaceTarget(TargetA);

                Log.Message($"Stinger: Starting dash to target through {dashPath.Count} cells");
            };

            stingerDashToil.tickAction = () =>
            {
                if (pawn.Dead || pawn.Downed)
                {
                    EndJobWith(JobCondition.Errored);
                    return;
                }

                // Check if dash is complete
                if (currentPathIndex >= dashPath.Count)
                {
                    // Perform final strike if we haven't already
                    if (!hasStruck && TargetA.HasThing && TargetA.Thing is Pawn target)
                    {
                        PerformStingerStrike(target);
                    }
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }

                // Dash forward rapidly
                if (Find.TickManager.TicksGame - lastMoveTick >= TicksBetweenCells)
                {
                    IntVec3 nextCell = dashPath[currentPathIndex];
                    
                    // Dash to next cell
                    if (WeaponDamageUtility.SafeTeleportPawn(pawn, nextCell))
                    {
                        Log.Message($"Stinger: Dashing to {nextCell} ({currentPathIndex + 1}/{dashPath.Count})");
                        
                        // Check if we're adjacent to target for the strike
                        if (TargetA.HasThing && TargetA.Thing is Pawn target && 
                            pawn.Position.AdjacentTo8WayOrInside(target.Position) && !hasStruck)
                        {
                            PerformStingerStrike(target);
                            hasStruck = true;
                        }
                        
                        currentPathIndex++;
                        lastMoveTick = Find.TickManager.TicksGame;
                    }
                    else
                    {
                        // Hit obstacle, end dash
                        Log.Warning($"Stinger: Hit obstacle at {nextCell}, ending dash");
                        EndJobWith(JobCondition.Succeeded);
                        return;
                    }
                }
            };

            stingerDashToil.defaultCompleteMode = ToilCompleteMode.Never;
            yield return stingerDashToil;
        }

        private void CalculateStingerDashPath()
        {
            dashPath = new List<IntVec3>();
            
            IntVec3 startPos = pawn.Position;
            IntVec3 targetPos = TargetA.Cell;
            
            // Create direct path to target
            List<IntVec3> lineCells = GenSight.PointsOnLineOfSight(startPos, targetPos).ToList();
            
            // Remove starting position
            if (lineCells.Count > 0 && lineCells[0] == startPos)
                lineCells.RemoveAt(0);

            // Add all cells up to the target
            foreach (IntVec3 cell in lineCells)
            {
                if (cell.InBounds(pawn.Map) && cell.Standable(pawn.Map))
                {
                    dashPath.Add(cell);
                }
                else
                {
                    break; // Stop at first impassable cell
                }
            }

            Log.Message($"Stinger: Dash path calculated with {dashPath.Count} cells");
        }

        private void PerformStingerStrike(Pawn target)
        {
            if (target == null || target.Dead) return;

            // Calculate enhanced damage for stinger strike
            float multiplier = DMCAbilitiesMod.settings?.stingerDamageMultiplier ?? 1.2f;
            var damageInfo = WeaponDamageUtility.CalculateMeleeDamage(pawn, multiplier);
            
            if (damageInfo.HasValue)
            {
                target.TakeDamage(damageInfo.Value);
                
                // Apply stagger effect
                if (target.health?.hediffSet != null)
                {
                    Hediff stagger = HediffMaker.MakeHediff(DMC_HediffDefOf.DMC_Stagger, target);
                    stagger.Severity = 0.5f; // Strong stagger from stinger
                    target.health.AddHediff(stagger);
                }

                // Create impact effects
                CreateStingerImpactEffects(target.Position);
                
                Log.Message($"Stinger: Strike hit {target.Label}");
            }
        }

        private void CreateStingerImpactEffects(IntVec3 position)
        {
            Map map = pawn.Map;
            if (map == null) return;

            // Impact effect
            FleckMaker.Static(position, map, FleckDefOf.PsycastAreaEffect, 2.0f);
            
            // Impact sound
            SoundStarter.PlayOneShot(SoundDefOf.Pawn_Melee_Punch_HitPawn, new TargetInfo(position, map));
        }
    }
}