using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace DMCAbilities
{
    public class Verb_RedHotNight : Verb_CastAbility
    {
        private const float BaseRadius = 4f; 
        private const int BaseOrbs = 5;
        private const float CastTimeTicks = 180f; // 3 seconds full cast
        private const float CooldownTicks = 480f; // 8 seconds

        public override bool CanHitTarget(LocalTargetInfo target)
        {
            // Check if mod and ability are enabled
            if (DMCAbilitiesMod.settings != null && 
                (!DMCAbilitiesMod.settings.modEnabled || !DMCAbilitiesMod.settings.redHotNightEnabled))
            {
                return false;
            }

            // Must have ranged weapon equipped - enhanced validation
            if (CasterPawn?.equipment?.Primary == null)
            {
                return false;
            }

            // Check if weapon is actually ranged (enhanced check)
            ThingWithComps weapon = CasterPawn.equipment.Primary;
            if (!weapon.def.IsRangedWeapon && !WeaponDamageUtility.IsRangedWeapon(weapon))
            {
                return false;
            }

            return base.CanHitTarget(target);
        }

        protected override bool TryCastShot()
        {
            // Ranged weapon validation with user feedback
            if (CasterPawn?.equipment?.Primary == null)
            {
                if (CasterPawn.IsPlayerControlled)
                {
                    Messages.Message("Red Hot Night requires a ranged weapon equipped.", MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }

            ThingWithComps weapon = CasterPawn.equipment.Primary;
            if (!weapon.def.IsRangedWeapon && !WeaponDamageUtility.IsRangedWeapon(weapon))
            {
                if (CasterPawn.IsPlayerControlled)
                {
                    Messages.Message("Red Hot Night can only be used with ranged weapons.", MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }

            // Execute the ability
            StartRedHotNight();
            return true;
        }

        private void StartRedHotNight()
        {
            Map map = CasterPawn.Map;
            IntVec3 targetCenter = currentTarget.Cell;

            // Calculate number of orbs based on shooting skill
            int shootingSkill = CasterPawn.skills?.GetSkill(SkillDefOf.Shooting)?.Level ?? 0;
            int maxOrbs = BaseOrbs + (int)(shootingSkill * 0.5f); // +0.5 orbs per skill level
            maxOrbs = Mathf.Min(maxOrbs, DMCAbilitiesMod.settings?.maxRedHotOrbs ?? 20);
            
            // Ensure we have a minimum number of orbs
            if (maxOrbs <= 0) maxOrbs = BaseOrbs;

            // Use proper job driver for sequential orb falling animation
            Job job = JobMaker.MakeJob(DMC_JobDefOf.DMC_RedHotNightCast, currentTarget);
            job.count = maxOrbs; // Pass orb count to job driver
            CasterPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        // Old direct spawning method removed - now using job driver for proper sequential animation

        private void ApplyBurnDebuffAtLocation(IntVec3 location, Map map)
        {
            // Find pawns in explosion area and apply burn
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(location, 2.5f, true))
            {
                if (!cell.InBounds(map)) continue;
                
                Pawn pawn = cell.GetFirstPawn(map);
                if (pawn != null && WeaponDamageUtility.ShouldTargetPawn(CasterPawn, pawn))
                {
                    // Apply Red Hot Burn debuff
                    Hediff burnHediff = HediffMaker.MakeHediff(DMC_HediffDefOf.DMC_RedHotBurn, pawn);
                    burnHediff.Severity = 1.0f;
                    pawn.health.AddHediff(burnHediff);
                }
            }
        }



        private void CreateCastEffects(IntVec3 center, Map map)
        {
            // Summon effect at center
            FleckMaker.ThrowFireGlow(center.ToVector3Shifted(), map, 2.5f);
            
            // Area indicator effect - show damage radius
            for (int i = 0; i < 16; i++)
            {
                float angle = i * 22.5f; // 360 degrees / 16 points
                Vector3 edgePos = center.ToVector3Shifted() + 
                    new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)) * BaseRadius;
                
                if (edgePos.ToIntVec3().InBounds(map))
                {
                    FleckMaker.ThrowHeatGlow(edgePos.ToIntVec3(), map, 1.0f);
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

        public override void DrawHighlight(LocalTargetInfo target)
        {
            // Draw the area of effect highlight
            if (target.IsValid && target.Cell.InBounds(CasterPawn?.Map))
            {
                // Draw radius ring for clear area visualization
                GenDraw.DrawRadiusRing(target.Cell, BaseRadius);
                
                // Also draw field edges for additional clarity
                GenDraw.DrawFieldEdges(GenRadial.RadialCellsAround(target.Cell, BaseRadius, true).ToList());
            }
        }
    }

    // Job driver for handling the sequential orb spawning with cast time system
    public class JobDriver_RedHotNightCast : JobDriver
    {
        private int orbsSpawned = 0;
        private int maxOrbs = 0;
        private int castTicks = 0;
        private const int FullCastTime = 180; // 3 seconds for full cast
        private const int MinCastTime = 30; // 0.5 seconds minimum before any orbs
        private const int DelayBetweenOrbs = 8; // Ticks between orb spawns

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);

            // Main casting toil
            yield return new Toil
            {
                initAction = () =>
                {
                    orbsSpawned = 0;
                    maxOrbs = job.count; // Set from verb
                    castTicks = 0;
                },
                tickAction = () =>
                {
                    castTicks++;

                    // Show casting effects
                    if (castTicks % 10 == 0)
                    {
                        CreateCastingEffects();
                    }

                    // Spawn orbs based on cast progress
                    if (castTicks >= MinCastTime && orbsSpawned < maxOrbs && castTicks % DelayBetweenOrbs == 0)
                    {
                        SpawnNextOrb();
                    }

                    // Complete when full cast time reached (always wait full duration)
                    if (castTicks >= FullCastTime)
                    {
                        ReadyForNextToil();
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Never,
                handlingFacing = true
            };

            // Completion toil
            yield return new Toil
            {
                initAction = () =>
                {
                    // Final effects
                    CreateCompletionEffects();
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        private void CreateCastingEffects()
        {
            float castProgress = Mathf.Clamp01((float)castTicks / FullCastTime);
            Vector3 casterPos = pawn.Position.ToVector3Shifted();

            // Escalating visual effects during cast
            Vector2 circleOffset = Rand.InsideUnitCircle * (1f + castProgress);
            Vector3 effectPos = casterPos + new Vector3(circleOffset.x, 0, circleOffset.y);
            
            FleckMaker.ThrowDustPuff(effectPos, pawn.Map, 0.8f + castProgress);

            if (castProgress > 0.3f && castTicks % 20 == 0)
            {
                FleckMaker.ThrowFireGlow(casterPos, pawn.Map, 1f + castProgress * 2f);
            }

            if (castProgress > 0.6f && castTicks % 15 == 0)
            {
                FleckMaker.ThrowHeatGlow(pawn.Position, pawn.Map, 0.5f + castProgress);
            }
        }

        private void SpawnNextOrb()
        {
            var targetCell = job.targetA.Cell;
            var map = pawn.Map;

            // Show Red Hot Night callout occasionally (since it spawns multiple orbs)
            DMCSpeechUtility.TryShowCallout(pawn, "DMC_RedHotNightActivation", 0.15f);

            // Calculate cast completion ratio for damage scaling
            float castProgress = Mathf.Clamp01((float)castTicks / FullCastTime);
            
            int shootingSkill = pawn.skills?.GetSkill(SkillDefOf.Shooting)?.Level ?? 0;
            
            // Damage scaling based on skill and cast progress
            float baseDamage = 25f;
            float skillMultiplier = 1f + (shootingSkill * 0.03f); // +3% per skill level
            float progressMultiplier = 0.5f + (castProgress * 0.5f); // 50%-100% based on cast progress
            float finalDamage = baseDamage * skillMultiplier * progressMultiplier;

            // Spread - tighter with more cast progress
            float baseSpread = 4f;
            float spreadReduction = castProgress * 2f; // Up to -2 tiles spread
            float finalSpread = Mathf.Max(1f, baseSpread - spreadReduction);

            // Calculate target position with spread
            Vector2 randomOffset = Rand.InsideUnitCircle * finalSpread;
            IntVec3 orbTarget = (targetCell.ToVector3() + new Vector3(randomOffset.x, 0, randomOffset.y)).ToIntVec3();
            orbTarget = orbTarget.ClampInsideMap(map);

            // Skip if would hit friendlies 
            if (DMCAbilitiesMod.settings?.disableFriendlyFire == true)
            {
                List<Thing> cellContents = map.thingGrid.ThingsListAtFast(orbTarget);
                bool skipCell = false;
                foreach (Thing thing in cellContents)
                {
                    if (thing is Pawn potentialTarget && !WeaponDamageUtility.ShouldTargetPawn(pawn, potentialTarget))
                    {
                        skipCell = true;
                        break;
                    }
                }
                if (skipCell) 
                {
                    orbsSpawned++; // Count as spawned to avoid infinite loop
                    return;
                }
            }

            // Spawn the orb
            SpawnRedOrb(orbTarget, finalDamage, map, orbsSpawned * 2); // Slight delay stagger
            orbsSpawned++;
        }

        private void SpawnRedOrb(IntVec3 targetCell, float damage, Map map, int delay = 0)
        {
            // Find random position within target area for this orb
            float radius = 4f;
            Vector2 randomOffset = Rand.InsideUnitCircle * radius;
            IntVec3 actualTarget = targetCell + new IntVec3(Mathf.RoundToInt(randomOffset.x), 0, Mathf.RoundToInt(randomOffset.y));
            
            // Make sure target is in bounds
            if (!actualTarget.InBounds(map))
            {
                actualTarget = targetCell;
            }

            // Use skill-based damage calculation with settings multiplier
            float multiplier = DMCAbilitiesMod.settings?.redHotNightDamageMultiplier ?? 1.0f;
            DamageInfo skillDamage = WeaponDamageUtility.CalculateSkillDamage(
                pawn: pawn, 
                multiplier: multiplier, 
                abilityName: "Red Hot Night",
                damageDef: DamageDefOf.Burn
            );
            
            // Create the projectile thing
            Thing projectileThing = ThingMaker.MakeThing(DMC_ThingDefOf.DMC_RedOrbProjectile);
            Projectile_RedOrb projectile = projectileThing as Projectile_RedOrb;
            
            if (projectile != null)
            {
                // Initialize with custom data including explosion damage
                projectile.InitializeForFalling(pawn, actualTarget, skillDamage.Amount, delay);
                
                // Spawn at target location (projectile will handle its own falling animation)
                GenSpawn.Spawn(projectile, actualTarget, map);
            }
            else
            {
                Log.Error("Red Hot Night: Failed to create falling orb projectile!");
            }
        }

        private void CreateCompletionEffects()
        {
            var targetCell = job.targetA.Cell;
            
            // Final completion sound and effect
            SoundStarter.PlayOneShot(SoundDefOf.Psycast_Skip_Exit, new TargetInfo(targetCell, pawn.Map));
            FleckMaker.ThrowExplosionCell(targetCell, pawn.Map, FleckDefOf.ExplosionFlash, Color.red);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref orbsSpawned, "orbsSpawned");
            Scribe_Values.Look(ref maxOrbs, "maxOrbs");
            Scribe_Values.Look(ref castTicks, "castTicks");
        }
    }
}