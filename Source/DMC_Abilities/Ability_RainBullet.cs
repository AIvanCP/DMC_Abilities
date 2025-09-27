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

            // Starting Rain Bullet cast

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



        public void FireDownwardBulletStorm(IntVec3 centerPos, IntVec3 originalPos)
        {
            var projectileDef = DefDatabase<ThingDef>.GetNamed("DMC_RainBulletProjectile", false);
            if (projectileDef == null)
            {
                Log.Error("Rain Bullet: Could not find DMC_RainBulletProjectile def");
                return;
            }

            // Calculate bullets based on skills (3-6 per area)
            int meleeSkill = CasterPawn?.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? 0;
            int shootingSkill = CasterPawn?.skills?.GetSkill(SkillDefOf.Shooting)?.Level ?? 0;
            
            int bulletsPerArea = Mathf.RoundToInt(3f + (shootingSkill * 0.15f)); // Uncapped scaling

            var map = CasterPawn.Map;
            
            // 1. DAMAGE ALONG TELEPORTATION PATH
            List<IntVec3> teleportPath = GenSight.PointsOnLineOfSight(originalPos, centerPos).ToList();
            foreach (IntVec3 pathCell in teleportPath)
            {
                if (pathCell == centerPos) continue; // Skip destination, handled separately
                
                // Each cell along path gets 3-6 bullets
                for (int i = 0; i < bulletsPerArea; i++)
                {
                    FireRainBulletAt(pathCell, map, projectileDef, false);
                }
            }
            
            // 2. ENHANCED BULLETS AT DESTINATION (with enemy targeting)
            float areaRadius = 3f + (meleeSkill * 0.1f);
            int destinationBullets = bulletsPerArea * 3; // More bullets at destination
            
            for (int i = 0; i < destinationBullets; i++)
            {
                IntVec3 targetCell = GetSmartTargetCell(centerPos, areaRadius, map);
                FireRainBulletAt(targetCell, map, projectileDef, true);
            }

            // Calculating path damage and bullets
        }

        private IntVec3 GetSmartTargetCell(IntVec3 center, float radius, Map map)
        {
            // 70% chance to target nearby enemies, 30% random
            if (Rand.Chance(0.7f))
            {
                // Look for enemies in area
                List<Pawn> nearbyEnemies = new List<Pawn>();
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
                {
                    if (!cell.InBounds(map)) continue;
                    
                    List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
                    foreach (Thing thing in things)
                    {
                        if (thing is Pawn pawn && pawn != CasterPawn && !pawn.Dead && 
                            pawn.HostileTo(CasterPawn))
                        {
                            nearbyEnemies.Add(pawn);
                        }
                    }
                }

                if (nearbyEnemies.Count > 0)
                {
                    return nearbyEnemies.RandomElement().Position;
                }
            }

            // Fallback to random position in area
            Vector2 randomOffset = Rand.InsideUnitCircle * radius;
            Vector3 randomPos = center.ToVector3() + new Vector3(randomOffset.x, 0, randomOffset.y);
            return randomPos.ToIntVec3().ClampInsideMap(map);
        }

        private void FireRainBulletAt(IntVec3 targetCell, Map map, ThingDef projectileDef, bool isDestinationShot)
        {
            // Small random offset for visual variety
            Vector2 offset = Rand.InsideUnitCircle * 0.5f;
            Vector3 finalTarget = targetCell.ToVector3() + new Vector3(offset.x, 0, offset.y);
            IntVec3 finalCell = finalTarget.ToIntVec3().ClampInsideMap(map);

            var projectile = (Projectile_RainBullet)GenSpawn.Spawn(projectileDef, finalCell, map);
            projectile.Initialize(CasterPawn, finalCell.ToVector3());
            projectile.Launch(CasterPawn, finalCell, finalCell, ProjectileHitFlags.IntendedTarget);
        }
    }

    public class JobDriver_RainBulletCast : JobDriver
    {
        private bool hasTeleported = false;
        private bool bulletStormFired = false;
        private IntVec3 originalPosition;

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
                // Starting DMC Rainstorm
                
                // Store original position before teleporting
                originalPosition = pawn.Position;
                
                // Teleport to target position first
                if (WeaponDamageUtility.SafeTeleportPawn(pawn, TargetA.Cell))
                {
                    hasTeleported = true;
                    // Teleported to destination
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
                        verb.FireDownwardBulletStorm(TargetA.Cell, originalPosition);
                        bulletStormFired = true;
                    }
                }

                // Wait a moment then end
                if (bulletStormFired && Find.TickManager.TicksGame % 60 == 0)
                {
                    // Rainstorm complete
                    EndJobWith(JobCondition.Succeeded);
                }
            };

            rainStormToil.defaultCompleteMode = ToilCompleteMode.Never;
            yield return rainStormToil;
        }

        // Old dash methods removed - now using teleport and downward shooting system
    }
}
