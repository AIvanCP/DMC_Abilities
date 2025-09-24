using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.AI;

namespace DMCAbilities
{
    public class Verb_HeavyRain : Verb_CastAbility
    {
        private const float BaseRadius = 6f; // Radius 5-7, using middle value
        private const int BaseProjectiles = 15;
        private const float DelayBetweenProjectiles = 3f; // Small delay in ticks
        private const float CooldownTicks = 780f; // 13 seconds

        protected override bool TryCastShot()
        {
            // Check if mod and ability are enabled
            if (DMCAbilitiesMod.settings != null && 
                (!DMCAbilitiesMod.settings.modEnabled || !DMCAbilitiesMod.settings.heavyRainEnabled))
            {
                return false;
            }

            StartHeavyRain();
            return true;
        }

        private void StartHeavyRain()
        {
            Map map = CasterPawn.Map;
            IntVec3 targetCenter = currentTarget.Cell;

            // Calculate number of projectiles based on melee skill
            int meleeSkill = CasterPawn.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? 0;
            int totalProjectiles = BaseProjectiles + (int)(meleeSkill * 0.75f);

            // Create visual and audio effects at cast
            CreateCastEffects(targetCenter, map);

            // Start the projectile rain job
            Job heavyRainJob = JobMaker.MakeJob(DMC_JobDefOf.DMC_HeavyRainCast);
            heavyRainJob.targetA = targetCenter;
            heavyRainJob.count = totalProjectiles;
            // Store caster reference in a different way since commTarget expects ICommunicable

            CasterPawn.jobs.StartJob(heavyRainJob, JobCondition.InterruptForced);
        }

        private void CreateCastEffects(IntVec3 center, Map map)
        {
            // Summon effect at center
            FleckMaker.Static(center, map, FleckDefOf.PsycastAreaEffect, 2.5f);
            
            // Area indicator effect
            for (int i = 0; i < 16; i++)
            {
                float angle = i * 22.5f; // 360 degrees / 16 points
                Vector3 edgePos = center.ToVector3Shifted() + 
                    new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)) * BaseRadius;
                
                if (edgePos.ToIntVec3().InBounds(map))
                {
                    FleckMaker.Static(edgePos.ToIntVec3(), map, FleckDefOf.PsycastAreaEffect, 1.0f);
                }
            }

            // Cast sound
            SoundStarter.PlayOneShot(SoundDefOf.Psycast_Skip_Pulse, new TargetInfo(center, map));
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = false;
            return BaseRadius;
        }
    }

    // Job driver for handling the sequential projectile spawning
    public class JobDriver_HeavyRainCast : JobDriver
    {
        private int projectilesSpawned = 0;
        private int totalProjectiles = 0;
        private int ticksSinceLastProjectile = 0;
        private const int DelayBetweenProjectiles = 3;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true; // No reservations needed
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_General.Do(delegate
            {
                // Initialize the rain sequence
                totalProjectiles = job.count;
                projectilesSpawned = 0;
                ticksSinceLastProjectile = 0;
            });

            Toil rainToil = new Toil();
            rainToil.tickAction = delegate()
            {
                if (projectilesSpawned >= totalProjectiles)
                {
                    // All regular projectiles spawned, check for special sword
                    TrySpawnSpecialSword();
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }

                ticksSinceLastProjectile++;
                if (ticksSinceLastProjectile >= DelayBetweenProjectiles)
                {
                    SpawnSpectralSword();
                    projectilesSpawned++;
                    ticksSinceLastProjectile = 0;
                }
            };

            rainToil.defaultCompleteMode = ToilCompleteMode.Never;
            rainToil.WithProgressBar(TargetIndex.A, () => (float)projectilesSpawned / totalProjectiles);

            yield return rainToil;
        }

        private void SpawnSpectralSword()
        {
            Map map = pawn.Map;
            IntVec3 targetCenter = job.targetA.Cell;

            // Find random position within the area
            Vector2 randomOffset2D = Rand.InsideUnitCircle * 6f; // Random within radius
            Vector3 randomOffset = new Vector3(randomOffset2D.x, 0, randomOffset2D.y);
            IntVec3 spawnCell = (targetCenter.ToVector3Shifted() + randomOffset).ToIntVec3();

            if (!spawnCell.InBounds(map))
            {
                spawnCell = targetCenter; // Fallback to center if out of bounds
            }

            // Create and spawn regular spectral sword projectile
            Thing projectileThing = ThingMaker.MakeThing(DMC_ThingDefOf.DMC_SpectralSwordProjectile);
            Projectile_SpectralSword projectile = projectileThing as Projectile_SpectralSword;

            if (projectile != null)
            {
                projectile.Initialize(pawn, spawnCell.ToVector3Shifted());
                GenSpawn.Spawn(projectile, spawnCell + IntVec3.North * 10, map); // Spawn high above

                // Visual effect for sword appearance
                FleckMaker.Static(spawnCell, map, FleckDefOf.PsycastAreaEffect, 0.8f);
                
                // Whoosh sound
                SoundStarter.PlayOneShot(SoundDefOf.Recipe_Surgery, new TargetInfo(spawnCell, map));
            }
        }

        private void TrySpawnSpecialSword()
        {
            // Calculate chance for special sword: 1 + (MeleeSkill * 0.3)
            int meleeSkill = pawn.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? 0;
            float specialChance = (1f + (meleeSkill * 0.3f)) / 100f;

            if (Rand.Chance(specialChance))
            {
                SpawnSpecialSword();
            }
        }

        private void SpawnSpecialSword()
        {
            Map map = pawn.Map;
            IntVec3 targetCenter = job.targetA.Cell;

            // Create and spawn special spectral sword at center
            Thing projectileThing = ThingMaker.MakeThing(DMC_ThingDefOf.DMC_SpectralSwordSpecialProjectile);
            Projectile_SpectralSwordSpecial projectile = projectileThing as Projectile_SpectralSwordSpecial;

            if (projectile != null)
            {
                GenSpawn.Spawn(projectile, targetCenter + IntVec3.North * 15, map); // Spawn higher for dramatic effect
                projectile.Launch(pawn, targetCenter, targetCenter, ProjectileHitFlags.IntendedTarget);

                // Enhanced visual and audio effects for special sword
                FleckMaker.Static(targetCenter, map, FleckDefOf.PsycastAreaEffect, 3.5f);
                
                // Multiple effect rings for dramatic impact
                for (int i = 1; i <= 3; i++)
                {
                    foreach (IntVec3 cell in GenRadial.RadialCellsAround(targetCenter, i * 2f, true))
                    {
                        if (cell.InBounds(map) && Rand.Chance(0.3f))
                        {
                            FleckMaker.Static(cell, map, FleckDefOf.PsycastAreaEffect, 1.2f);
                        }
                    }
                }

                // Enhanced sound effect
                SoundStarter.PlayOneShot(SoundDefOf.Psycast_Skip_Pulse, new TargetInfo(targetCenter, map));
            }
        }
    }
}