using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.AI;

namespace DMCAbilities
{
    public class Verb_RainBullet : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            // Check if mod and ability are enabled
            if (DMCAbilitiesMod.settings != null && 
                (!DMCAbilitiesMod.settings.modEnabled || !DMCAbilitiesMod.settings.rainBulletEnabled))
            {
                return false;
            }

            // Check for pistol weapon requirement
            if (!WeaponDamageUtility.HasPistolWeapon(CasterPawn))
            {
                Messages.Message(WeaponDamageUtility.GetNoPistolWeaponMessage(), 
                    CasterPawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            Log.Message($"Rain Bullet: Starting cast job for {CasterPawn} at target {CurrentTarget}");

            var job = JobMaker.MakeJob(DMC_JobDefOf.DMC_RainBulletCast, CurrentTarget);
            job.verbToUse = this;
            CasterPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            return true;
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = false;
            return verbProps.range;
        }



        public void FireBulletStorm(IntVec3 targetCell)
        {
            if (CasterPawn == null || CasterPawn.Dead) return;

            var projectileDef = DefDatabase<ThingDef>.GetNamed("DMC_RainBulletProjectile", false);
            if (projectileDef == null)
            {
                Log.Error("Rain Bullet: Could not find DMC_RainBulletProjectile def");
                return;
            }

            // Calculate total bullets based on skills like before
            int meleeSkill = CasterPawn?.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? 0;
            int shootingSkill = CasterPawn?.skills?.GetSkill(SkillDefOf.Shooting)?.Level ?? 0;
            
            float jumpDistance = Vector3.Distance(CasterPawn.Position.ToVector3(), targetCell.ToVector3());
            float meleeModifier = 1f + (meleeSkill * 0.05f);
            float effectiveDistance = jumpDistance * meleeModifier;
            
            float bulletsPerCell = 3f + (shootingSkill * 0.15f);
            bulletsPerCell = Mathf.Clamp(bulletsPerCell, 3f, 6f);
            
            int totalBullets = Mathf.RoundToInt(effectiveDistance * bulletsPerCell);
            totalBullets = Mathf.Clamp(totalBullets, 8, 30);

            var map = CasterPawn.Map;
            
            // Fire all bullets rapidly in a storm pattern (like DMC)
            for (int i = 0; i < totalBullets; i++)
            {
                // Random area around target for bulletstorm effect (3 cell radius)
                Vector2 randomOffset = Rand.InsideUnitCircle * Rand.Range(1f, 3f);
                Vector3 impactPos = targetCell.ToVector3() + new Vector3(randomOffset.x, 0, randomOffset.y);
                IntVec3 impactCell = impactPos.ToIntVec3().ClampInsideMap(map);

                // Spawn from above (simulate aerial shooting)
                IntVec3 skyPos = new IntVec3(impactCell.x, 0, impactCell.z + 8); // High above target
                
                var projectile = (Projectile_RainBullet)GenSpawn.Spawn(projectileDef, skyPos, map);
                projectile.Initialize(CasterPawn, impactCell.ToVector3());
                projectile.Launch(CasterPawn, impactCell, impactCell, ProjectileHitFlags.IntendedTarget);
            }

            Log.Message($"Rain Bullet: Fired bulletstorm of {totalBullets} bullets at {targetCell}");
        }

        public bool TryTeleportCaster(IntVec3 targetCell)
        {
            Log.Message($"Rain Bullet: Attempting to teleport {CasterPawn} to {targetCell}");
            return WeaponDamageUtility.SafeTeleportPawn(CasterPawn, targetCell);
        }
    }

    public class JobDriver_RainBulletCast : JobDriver
    {
        private bool bulletStormFired = false;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);

            // Simple DMC-style: fire bulletstorm, then teleport
            var castToil = new Toil();
            castToil.initAction = () =>
            {
                var verb = job.verbToUse as Verb_RainBullet;
                if (verb == null)
                {
                    Log.Error("Rain Bullet: Invalid verb in job");
                    EndJobWith(JobCondition.Errored);
                    return;
                }

                Log.Message($"Rain Bullet: Starting DMC-style bulletstorm");
                
                // Fire the entire bulletstorm immediately
                verb.FireBulletStorm(TargetA.Cell);
                bulletStormFired = true;
            };

            castToil.tickAction = () =>
            {
                var verb = job.verbToUse as Verb_RainBullet;
                if (verb == null || pawn.Dead || pawn.Downed)
                {
                    EndJobWith(JobCondition.Errored);
                    return;
                }

                // Wait 1 second for bullets to rain down, then teleport
                if (bulletStormFired && Find.TickManager.TicksGame % 60 == 0)
                {
                    Log.Message($"Rain Bullet: Bulletstorm complete, teleporting to {TargetA.Cell}");
                    if (verb.TryTeleportCaster(TargetA.Cell))
                    {
                        Log.Message($"Rain Bullet: Teleport successful");
                        EndJobWith(JobCondition.Succeeded);
                    }
                    else
                    {
                        Log.Warning($"Rain Bullet: Teleport failed to {TargetA.Cell}");
                        EndJobWith(JobCondition.Incompletable);
                    }
                }
            };

            castToil.defaultCompleteMode = ToilCompleteMode.Never;
            yield return castToil;
        }
    }
}
