using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace DMCAbilities
{
    public class Verb_JudgementCut : Verb_CastAbility
    {
        private int warmupTicksLeft = 0;
        private IntVec3 targetPosition;
        private bool isWarmingUp = false;

        public override bool Available()
        {
            return base.Available() && !isWarmingUp;
        }

        protected override bool TryCastShot()
        {
            // Check if mod and ability are enabled
            if (DMCAbilitiesMod.settings != null && 
                (!DMCAbilitiesMod.settings.modEnabled || !DMCAbilitiesMod.settings.judgementCutEnabled))
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
            StartWarmup();
            return true;
        }

        private void StartWarmup()
        {
            isWarmingUp = true;
            warmupTicksLeft = Rand.RangeInclusive(30, 60); // 0.5-1 second warmup
            
            // Show warmup effect
            FleckMaker.Static(CasterPawn.Position, CasterPawn.Map, FleckDefOf.PsycastAreaEffect, 2f);
            
            // Create a job to handle the warmup
            Job warmupJob = JobMaker.MakeJob(DMC_JobDefOf.DMC_JudgementCutWarmup);
            warmupJob.verbToUse = this;
            CasterPawn.jobs.TryTakeOrderedJob(warmupJob, JobTag.Misc);
        }

        public void TickWarmup()
        {
            if (!isWarmingUp)
                return;

            warmupTicksLeft--;
            
            if (warmupTicksLeft <= 0)
            {
                CompleteJudgementCut();
                
                // End the warmup job
                if (CasterPawn?.jobs?.curJob?.def?.defName == "DMC_JudgementCutWarmup")
                {
                    CasterPawn.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            }
            else
            {
                // Show ongoing warmup effect
                if (warmupTicksLeft % 10 == 0)
                {
                    FleckMaker.ThrowDustPuff(CasterPawn.Position.ToVector3Shifted(), CasterPawn.Map, 0.8f);
                }
            }
        }

        private void CompleteJudgementCut()
        {
            isWarmingUp = false;
            
            if (CasterPawn == null || CasterPawn.Map == null || !targetPosition.IsValid)
                return;

            // Additional safety check - make sure pawn is still alive and conscious
            if (CasterPawn.Dead || CasterPawn.Downed)
                return;

            // Determine number of slashes
            int slashCount = DetermineSlashCount();
            
            // Create slashes
            for (int i = 0; i < slashCount; i++)
            {
                IntVec3 slashPos = GetSlashPosition(i, slashCount);
                if (slashPos.IsValid && slashPos.InBounds(CasterPawn.Map))
                {
                    CreateJudgementSlash(slashPos);
                }
            }
        }

        private int DetermineSlashCount()
        {
            float rand = Rand.Value;
            if (rand <= 0.05f) return 3; // 5% chance for 3 slashes
            if (rand <= 0.20f) return 2; // 15% chance for 2 slashes
            return 1; // 80% chance for 1 slash
        }

        private IntVec3 GetSlashPosition(int slashIndex, int totalSlashes)
        {
            if (totalSlashes == 1)
                return targetPosition;

            // For multiple slashes, spread them around the target
            Vector3 basePos = targetPosition.ToVector3();
            float angle = (360f / totalSlashes) * slashIndex;
            float distance = Rand.Range(0.5f, 1.5f);
            
            Vector3 offset = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
                0f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * distance
            );

            IntVec3 slashPos = (basePos + offset).ToIntVec3();
            
            // Ensure position is valid
            if (!slashPos.InBounds(CasterPawn.Map))
                slashPos = targetPosition;
                
            return slashPos;
        }

        private void CreateJudgementSlash(IntVec3 position)
        {
            Map map = CasterPawn.Map;
            
            // Create visual effect
            FleckMaker.Static(position, map, FleckDefOf.PsycastAreaEffect, 3f);
            
            // Create explosion-like effect with damage radius of 2
            GenExplosion.DoExplosion(
                center: position,
                map: map,
                radius: 2f,
                damType: DamageDefOf.Cut, // Will be overridden by custom damage
                instigator: CasterPawn,
                damAmount: 0, // We'll apply custom damage
                armorPenetration: 0f,
                explosionSound: null,
                weapon: null,
                projectile: null,
                intendedTarget: null,
                postExplosionSpawnThingDef: null,
                postExplosionSpawnChance: 0f,
                postExplosionSpawnThingCount: 1,
                preExplosionSpawnThingDef: null,
                preExplosionSpawnChance: 0f,
                preExplosionSpawnThingCount: 1,
                chanceToStartFire: 0f,
                damageFalloff: false,
                direction: null,
                ignoredThings: null,
                doVisualEffects: false, // We're doing our own visuals
                propagationSpeed: 1f
            );

            // Apply custom weapon-based damage to all pawns in radius
            ApplyJudgementDamage(position, 2f);
        }

        private void ApplyJudgementDamage(IntVec3 center, float radius)
        {
            Map map = CasterPawn.Map;
            
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(center, map, radius, true))
            {
                if (thing is Pawn targetPawn && targetPawn != CasterPawn)
                {
                    // Calculate damage without multiplier (base weapon damage)
                    var damageInfo = WeaponDamageUtility.CalculateMeleeDamage(CasterPawn, 1f);
                    if (damageInfo.HasValue)
                    {
                        // Apply damage
                        targetPawn.TakeDamage(damageInfo.Value);
                        
                        // Create impact effect on each target
                        FleckMaker.Static(targetPawn.Position, map, FleckDefOf.MicroSparks, 1f);
                    }
                }
            }
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = false;
            return 2f; // Show the damage radius
        }

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.ValidateTarget(target, showMessages))
                return false;

            if (isWarmingUp)
            {
                if (showMessages)
                    Messages.Message("DMC_AlreadyWarmingUp".Translate(), 
                        MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return true;
        }
    }

    public class JobDriver_JudgementCutWarmup : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil warmupToil = new Toil
            {
                initAction = delegate
                {
                    // Set pawn stance
                    pawn.stances.SetStance(new Stance_Warmup(1, null, null));
                },
                tickAction = delegate
                {
                    // Safety check - if pawn or job is invalid, end the job
                    if (pawn == null || job?.verbToUse == null)
                    {
                        EndJobWith(JobCondition.Errored);
                        return;
                    }

                    if (job.verbToUse is Verb_JudgementCut judgementCutVerb)
                    {
                        judgementCutVerb.TickWarmup();
                    }
                    else
                    {
                        EndJobWith(JobCondition.Errored);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Never
            };

            warmupToil.AddFailCondition(() => pawn.Drafted && pawn.jobs.curJob != job);
            yield return warmupToil;
        }
    }
}