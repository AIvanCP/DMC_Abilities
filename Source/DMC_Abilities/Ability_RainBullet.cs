using System.Collections.Generic;
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

            // Check range (6-10 cells)
            float distance = CasterPawn.Position.DistanceTo(currentTarget.Cell);
            if (distance < 6f || distance > 10f)
            {
                Messages.Message("Rain Bullet target must be 6-10 cells away.", 
                    CasterPawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // Check if target destination is valid for teleportation
            IntVec3 safeDestination = WeaponDamageUtility.FindSafeTeleportPosition(currentTarget.Cell, CasterPawn.Map, CasterPawn, 3);
            if (safeDestination == IntVec3.Invalid)
            {
                Messages.Message("Rain Bullet: No safe landing area found near target.", 
                    CasterPawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            StartRainBullet();
            return true;
        }

        private void StartRainBullet()
        {
            Map map = CasterPawn.Map;
            IntVec3 targetCell = currentTarget.Cell;

            // Calculate bullets per cell based on shooting skill (3-6)
            // Higher shooting skill = more precise shots = more bullets per cell
            int shootingSkill = CasterPawn.skills?.GetSkill(SkillDefOf.Shooting)?.Level ?? 0;
            int bulletsPerCell = Mathf.Clamp(3 + (shootingSkill / 4), 3, 6);

            // Calculate jump path distance for total projectile calculation
            float jumpDistance = CasterPawn.Position.DistanceTo(targetCell);
            int pathCells = Mathf.CeilToInt(jumpDistance); // Number of cells in jump path

            // Create cast effects
            CreateCastEffects(CasterPawn.Position, map);

            // Start the rain bullet job - will define job def in XML
            JobDef rainBulletJobDef = DefDatabase<JobDef>.GetNamedSilentFail("DMC_RainBulletCast");
            if (rainBulletJobDef != null)
            {
                Job rainBulletJob = JobMaker.MakeJob(rainBulletJobDef);
                rainBulletJob.targetA = targetCell;
                rainBulletJob.count = bulletsPerCell; // Store bullets per cell
                rainBulletJob.targetB = CasterPawn.Position; // Store starting position
                CasterPawn.jobs.StartJob(rainBulletJob, JobCondition.InterruptForced);
            }
        }

        private void CreateCastEffects(IntVec3 casterPos, Map map)
        {
            // Jump effect at caster position
            FleckMaker.Static(casterPos, map, FleckDefOf.PsycastAreaEffect, 1.2f);
            
            // Jump sound
            SoundStarter.PlayOneShot(SoundDefOf.Pawn_Melee_Punch_Miss, new TargetInfo(casterPos, map));
        }
    }

    // Job driver for handling the Rain Bullet sequence
    public class JobDriver_RainBulletCast : JobDriver
    {
        private int bulletsShot = 0;
        private int bulletsPerCell = 0;
        private int ticksSinceLastBullet = 0;
        private const int DelayBetweenBullets = 3; // Faster rate for continuous fire
        private bool hasJumped = false;
        private List<IntVec3> jumpPath = new List<IntVec3>();
        private int currentPathIndex = 0;
        private int bulletsInCurrentCell = 0;
        private const int MaxTotalBullets = 50; // Safety cap to prevent performance issues

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true; // No reservations needed for this ability
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil bulletToil = new Toil()
            {
                initAction = () =>
                {
                    bulletsPerCell = job.count; // Bullets to fire per cell
                    bulletsShot = 0;
                    ticksSinceLastBullet = 0;
                    hasJumped = false;
                    currentPathIndex = 0;
                    bulletsInCurrentCell = 0;
                    
                    // Calculate jump path from start to target
                    IntVec3 startPos = job.targetB.Cell;
                    IntVec3 endPos = job.targetA.Cell;
                    jumpPath = CalculateJumpPath(startPos, endPos);
                },
                tickAction = () =>
                {
                    // Jump effect on first tick
                    if (!hasJumped)
                    {
                        CreateJumpEffect();
                        hasJumped = true;
                    }

                    // Safety check - prevent excessive bullets
                    if (bulletsShot >= MaxTotalBullets)
                    {
                        ReadyForNextToil();
                        return;
                    }

                    // Check if we've completed the entire jump path
                    if (currentPathIndex >= jumpPath.Count)
                    {
                        // Teleport pawn to target position (like Dante landing after Rain Bullet)
                        TeleportPawnToTarget();
                        ReadyForNextToil();
                        return;
                    }

                    ticksSinceLastBullet++;
                    if (ticksSinceLastBullet >= DelayBetweenBullets)
                    {
                        // Fire bullet for current path cell
                        IntVec3 currentCell = jumpPath[currentPathIndex];
                        ShootRainBulletAtCell(currentCell);
                        bulletsShot++;
                        bulletsInCurrentCell++;
                        ticksSinceLastBullet = 0;

                        // Check if we've fired enough bullets for this cell
                        if (bulletsInCurrentCell >= bulletsPerCell)
                        {
                            // Move to next cell in path
                            currentPathIndex++;
                            bulletsInCurrentCell = 0;
                        }
                    }
                }
            };

            bulletToil.defaultCompleteMode = ToilCompleteMode.Never;
            bulletToil.WithProgressBar(TargetIndex.A, () => jumpPath.Count > 0 ? (float)currentPathIndex / jumpPath.Count : 0f);

            yield return bulletToil;
        }

        private List<IntVec3> CalculateJumpPath(IntVec3 start, IntVec3 end)
        {
            List<IntVec3> path = new List<IntVec3>();
            
            // Simple line-drawing algorithm to get cells between start and end
            int dx = Mathf.Abs(end.x - start.x);
            int dz = Mathf.Abs(end.z - start.z);
            int x = start.x;
            int z = start.z;
            int n = 1 + dx + dz;
            int x_inc = (end.x > start.x) ? 1 : -1;
            int z_inc = (end.z > start.z) ? 1 : -1;
            int error = dx - dz;
            dx *= 2;
            dz *= 2;

            for (; n > 0; --n)
            {
                path.Add(new IntVec3(x, 0, z));

                if (error > 0)
                {
                    x += x_inc;
                    error -= dz;
                }
                else
                {
                    z += z_inc;
                    error += dx;
                }
            }

            return path;
        }

        private void CreateJumpEffect()
        {
            Map map = pawn.Map;
            if (map != null)
            {
                // Visual jump effect
                FleckMaker.Static(pawn.Position, map, FleckDefOf.ExplosionFlash, 1.5f);
                
                // Jump sound
                SoundStarter.PlayOneShot(SoundDefOf.Pawn_Melee_Punch_Miss, new TargetInfo(pawn.Position, map));
            }
        }

        private void ShootRainBulletAtCell(IntVec3 targetCell)
        {
            Map map = pawn.Map;

            // Create and spawn rain bullet projectile - will define in XML
            ThingDef projectileDef = DefDatabase<ThingDef>.GetNamedSilentFail("DMC_RainBulletProjectile");
            if (projectileDef == null) return; // Projectile def not found
            
            Thing projectileThing = ThingMaker.MakeThing(projectileDef);
            Projectile_RainBullet projectile = projectileThing as Projectile_RainBullet;

            if (projectile != null)
            {
                projectile.Initialize(pawn, targetCell.ToVector3Shifted());
                
                // Calculate jump height/distance based on melee skill
                // Higher melee skill = better body control = higher/further jump
                int meleeSkill = pawn.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? 0;
                int jumpHeight = Mathf.Clamp(3 + (meleeSkill / 3), 3, 8); // Height ranges from 3 to 8 based on skill
                
                // Launch from above the current cell in jump path
                IntVec3 launchPos = targetCell + IntVec3.North * jumpHeight;
                
                GenSpawn.Spawn(projectile, launchPos, map);

                // Enhanced muzzle flash effect at current jump position
                FleckMaker.Static(targetCell, map, FleckDefOf.ShotFlash, 0.8f + (meleeSkill * 0.01f));
                
                // Gunshot sound - slightly quieter for continuous fire
                SoundStarter.PlayOneShot(SoundDefOf.Pawn_Melee_Punch_HitPawn, new TargetInfo(targetCell, map));
            }
        }

        private void TeleportPawnToTarget()
        {
            IntVec3 targetPos = job.targetA.Cell;
            
            // Use the shared safe teleport utility
            bool teleportSucceeded = WeaponDamageUtility.SafeTeleportPawn(pawn, targetPos, true);
            
            if (teleportSucceeded)
            {
                // Add extra dramatic landing effects for Rain Bullet
                Map map = pawn.Map;
                FleckMaker.Static(pawn.Position, map, FleckDefOf.ExplosionFlash, 2.0f);
            }
            // The utility already handles failure cases and shows appropriate messages
        }


    }
}