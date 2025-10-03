using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace DMCAbilities
{
    public class Verb_Drive : Verb_CastAbility
    {
        private int projectilesToLaunch = 0;
        private int projectilesLaunched = 0;
        private int launchDelayTicks = 0;
        private IntVec3 targetPosition;

        protected override bool TryCastShot()
        {
            // Check if mod and ability are enabled
            if (DMCAbilitiesMod.settings != null && 
                (!DMCAbilitiesMod.settings.modEnabled || !DMCAbilitiesMod.settings.driveEnabled))
            {
                return false;
            }

            if (!WeaponDamageUtility.HasMeleeWeapon(CasterPawn))
            {
                Messages.Message(WeaponDamageUtility.GetNoMeleeWeaponMessage(), 
                    CasterPawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            targetPosition = currentTarget.Cell;
            StartDriveSequence();
            return true;
        }

        private void StartDriveSequence()
        {
            if (CasterPawn == null || CasterPawn.Map == null)
                return;

            // Determine number of projectiles (same RNG as Judgement Cut)
            projectilesToLaunch = DetermineProjectileCount();
            projectilesLaunched = 0;
            launchDelayTicks = 0;

            // Show initial effect
            if (CasterPawn.Position.IsValid)
            {
                FleckMaker.Static(CasterPawn.Position, CasterPawn.Map, FleckDefOf.PsycastAreaEffect, 2.5f);
                SoundStarter.PlayOneShot(SoundDefOf.Psycast_Skip_Pulse, new TargetInfo(CasterPawn.Position, CasterPawn.Map));
            }

            // Launch first projectile immediately
            LaunchDriveProjectile(0);

            // If more projectiles, start the delayed launch job
            if (projectilesToLaunch > 1)
            {
                Job delayedLaunchJob = JobMaker.MakeJob(DMC_JobDefOf.DMC_DriveDelayedLaunch);
                delayedLaunchJob.verbToUse = this;
                CasterPawn.jobs.TryTakeOrderedJob(delayedLaunchJob, JobTag.Misc);
            }
        }

        private int DetermineProjectileCount()
        {
            float rand = Rand.Value;
            if (rand <= 0.05f) return 3; // 5% chance for 3 projectiles
            if (rand <= 0.20f) return 2; // 15% chance for 2 projectiles
            return 1; // 80% chance for 1 projectile
        }

        private void LaunchDriveProjectile(int projectileIndex)
        {
            if (CasterPawn == null || CasterPawn.Map == null || !targetPosition.IsValid)
                return;
                
            projectilesLaunched++;
            bool isFinalProjectile = (projectileIndex == projectilesToLaunch - 1) && projectilesToLaunch >= 2;

            // Show Drive callout when launching projectiles (only for first projectile to avoid spam)
            if (projectileIndex == 0)
            {
                DMCSpeechUtility.TryShowCallout(CasterPawn, "DMC_DriveActivation", 0.3f);
            }

            // Calculate launch direction
            Vector3 casterPos = CasterPawn.Position.ToVector3Shifted();
            Vector3 targetPos = targetPosition.ToVector3Shifted();
            Vector3 direction = (targetPos - casterPos).normalized;
            
            // Calculate maximum range target (extend projectile to max distance)
            float maxRange = 25f; // Max projectile range
            Vector3 maxRangeTarget = casterPos + direction * maxRange;
            IntVec3 finalTarget = maxRangeTarget.ToIntVec3();

            // Create projectile
            Thing projectileThing = ThingMaker.MakeThing(DMC_ThingDefOf.DMC_DriveSlashProjectile);
            Projectile_DriveSlash projectile = projectileThing as Projectile_DriveSlash;

            if (projectile != null)
            {
                // Set projectile properties
                float damageMultiplier = DMCAbilitiesMod.settings?.driveDamageMultiplier ?? 1.0f;
                if (isFinalProjectile)
                {
                    damageMultiplier *= 1.5f; // Final projectile deals 50% more damage
                    projectile.SetFinalProjectile(true, damageMultiplier);
                }
                else
                {
                    projectile.SetFinalProjectile(false, damageMultiplier);
                }

                // Launch projectile to max range
                GenSpawn.Spawn(projectile, CasterPawn.Position, CasterPawn.Map);
                projectile.Launch(CasterPawn, finalTarget, finalTarget, ProjectileHitFlags.IntendedTarget);

                // Visual and sound effects
                CreateDriveEffects(projectileIndex, isFinalProjectile);
            }
        }

        private void CreateDriveEffects(int projectileIndex, bool isFinal)
        {
            Map map = CasterPawn.Map;
            Vector3 casterPos = CasterPawn.Position.ToVector3Shifted();

            // Create slash effect at caster position
            float effectScale = 2f + (projectileIndex * 0.3f);
            FleckMaker.Static(CasterPawn.Position, map, FleckDefOf.PsycastAreaEffect, effectScale);

            // Enhanced effects for final projectile
            if (isFinal)
            {
                // Massive visual effects for triple Drive finale
                FleckMaker.Static(CasterPawn.Position, map, FleckDefOf.PsycastAreaEffect, 3.5f);
                FleckMaker.ThrowLightningGlow(casterPos, map, 2f);
                FleckMaker.Static(CasterPawn.Position, map, FleckDefOf.ExplosionFlash, 2.5f);
                
                // Special sound and message for triple Drive
                SoundStarter.PlayOneShot(SoundDefOf.Psycast_Skip_Exit, new TargetInfo(CasterPawn.Position, map));
                
                
            }
            else
            {
                SoundStarter.PlayOneShot(SoundDefOf.Pawn_Melee_Punch_HitPawn, new TargetInfo(CasterPawn.Position, map));
            }
        }

        public void TickDelayedLaunch()
        {
            if (CasterPawn == null || CasterPawn.Map == null)
                return;
                
            if (projectilesLaunched >= projectilesToLaunch)
            {
                // All projectiles launched, end job
                if (CasterPawn?.jobs?.curJob?.def == DMC_JobDefOf.DMC_DriveDelayedLaunch)
                {
                    CasterPawn.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
                return;
            }

            launchDelayTicks++;

            // Launch next projectile every 20 ticks (1/3 second delay)
            if (launchDelayTicks >= 20)
            {
                LaunchDriveProjectile(projectilesLaunched);
                launchDelayTicks = 0;
            }
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = false;
            return 25f; // Show max range radius during targeting
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            // Draw the drive line from caster to max range in target direction
            if (target.IsValid && CasterPawn != null && CasterPawn.Position.IsValid)
            {
                Vector3 casterPos = CasterPawn.Position.ToVector3Shifted();
                Vector3 targetPos = target.Cell.ToVector3Shifted();
                Vector3 direction = (targetPos - casterPos).normalized;
                
                // Calculate max range endpoint
                float maxRange = 25f;
                Vector3 endPos = casterPos + direction * maxRange;
                IntVec3 endCell = endPos.ToIntVec3();
                
                if (endCell.InBounds(CasterPawn.Map))
                {
                    // Draw line cells from caster to end position
                    List<IntVec3> lineCells = GenSight.PointsOnLineOfSight(CasterPawn.Position, endCell).ToList();
                    GenDraw.DrawFieldEdges(lineCells);
                }
            }
        }

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.ValidateTarget(target, showMessages))
                return false;

            // Allow targeting any location
            return true;
        }
    }

    // Job driver for delayed projectile launching
    public class JobDriver_DriveDelayedLaunch : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil delayedLaunchToil = new Toil
            {
                initAction = delegate
                {
                    // Set pawn stance
                    pawn.stances.SetStance(new Stance_Warmup(1, null, null));
                },
                tickAction = delegate
                {
                    // Safety check
                    if (pawn == null || job?.verbToUse == null)
                    {
                        EndJobWith(JobCondition.Errored);
                        return;
                    }

                    if (job.verbToUse is Verb_Drive driveVerb)
                    {
                        driveVerb.TickDelayedLaunch();
                    }
                    else
                    {
                        EndJobWith(JobCondition.Errored);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Never
            };

            delayedLaunchToil.AddFailCondition(() => pawn.Drafted && pawn.jobs.curJob != job);
            yield return delayedLaunchToil;
        }
    }
}