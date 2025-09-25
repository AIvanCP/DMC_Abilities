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



        public void FireDownwardBulletStorm(IntVec3 centerPos)
        {
            if (CasterPawn == null || CasterPawn.Dead) return;

            var projectileDef = DefDatabase<ThingDef>.GetNamed("DMC_RainBulletProjectile", false);
            if (projectileDef == null)
            {
                Log.Error("Rain Bullet: Could not find DMC_RainBulletProjectile def");
                return;
            }

            // Calculate bullets based on skills (3-6 per area)
            int meleeSkill = CasterPawn?.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? 0;
            int shootingSkill = CasterPawn?.skills?.GetSkill(SkillDefOf.Shooting)?.Level ?? 0;
            
            // Calculate area coverage (like the old system)
            float areaRadius = 3f + (meleeSkill * 0.1f); // Bigger area with skill
            int bulletsPerArea = Mathf.RoundToInt(3f + (shootingSkill * 0.15f));
            bulletsPerArea = Mathf.Clamp(bulletsPerArea, 3, 6);
            
            int totalBullets = Mathf.RoundToInt(areaRadius * areaRadius * bulletsPerArea * 0.5f);
            totalBullets = Mathf.Clamp(totalBullets, 12, 40);

            var map = CasterPawn.Map;
            
            // Fire bullets in area around caster (like DMC Rainstorm)
            for (int i = 0; i < totalBullets; i++)
            {
                // Random spread around caster position
                Vector2 randomOffset = Rand.InsideUnitCircle * areaRadius;
                Vector3 impactPos = centerPos.ToVector3() + new Vector3(randomOffset.x, 0, randomOffset.y);
                IntVec3 impactCell = impactPos.ToIntVec3().ClampInsideMap(map);

                // Spawn bullets from above (simulate downward rain)
                IntVec3 skyPos = new IntVec3(impactCell.x, 0, impactCell.z + 8);
                
                var projectile = (Projectile_RainBullet)GenSpawn.Spawn(projectileDef, skyPos, map);
                projectile.Initialize(CasterPawn, impactCell.ToVector3());
                projectile.Launch(CasterPawn, impactCell, impactCell, ProjectileHitFlags.IntendedTarget);
            }

            Log.Message($"Rain Bullet: Fired downward bullet storm of {totalBullets} bullets around {centerPos}");
        }
    }

    public class JobDriver_RainBulletCast : JobDriver
    {
        private bool hasTeleported = false;
        private bool bulletStormFired = false;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);

            // DMC Rainstorm style: teleport above and shoot downward
            var rainStormToil = new Toil();
            rainStormToil.initAction = () =>
            {
                Log.Message($"Rain Bullet: Starting DMC Rainstorm - teleport and shoot downward");
                
                // Teleport to target position first
                if (WeaponDamageUtility.SafeTeleportPawn(pawn, TargetA.Cell))
                {
                    hasTeleported = true;
                    Log.Message($"Rain Bullet: Teleported to {TargetA.Cell}");
                }
                else
                {
                    // If teleport fails, end ability
                    Log.Warning($"Rain Bullet: Failed to teleport to {TargetA.Cell}");
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
            };

            rainStormToil.tickAction = () =>
            {
                if (pawn.Dead || pawn.Downed)
                {
                    EndJobWith(JobCondition.Errored);
                    return;
                }

                // After teleporting, fire bullet storm downward
                if (hasTeleported && !bulletStormFired)
                {
                    var verb = job.verbToUse as Verb_RainBullet;
                    if (verb != null)
                    {
                        verb.FireDownwardBulletStorm(TargetA.Cell);
                        bulletStormFired = true;
                    }
                }

                // Wait a moment then end
                if (bulletStormFired && Find.TickManager.TicksGame % 60 == 0)
                {
                    Log.Message($"Rain Bullet: Rainstorm complete");
                    EndJobWith(JobCondition.Succeeded);
                }
            };

            rainStormToil.defaultCompleteMode = ToilCompleteMode.Never;
            yield return rainStormToil;
        }

        // Old dash methods removed - now using teleport and downward shooting system
    }
}
