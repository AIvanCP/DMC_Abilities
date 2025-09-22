using System.Collections.Generic;
using System.Linq;
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
        private int currentSlashCount = 0; // Track current slash for visual effects

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
            warmupTicksLeft = Rand.RangeInclusive(15, 25); // Faster: 0.25-0.4 second warmup
            
            // Show warmup effect
            FleckMaker.ThrowDustPuff(CasterPawn.Position.ToVector3Shifted(), CasterPawn.Map, 2f);
            
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
            currentSlashCount = 0; // Reset slash counter
            
            if (CasterPawn == null || CasterPawn.Map == null || !targetPosition.IsValid)
                return;

            // Additional safety check - make sure pawn is still alive and conscious
            if (CasterPawn.Dead || CasterPawn.Downed)
                return;

            // Determine number of slashes
            int slashCount = DetermineSlashCount();
            
            // Create all slashes at the target location (DMC 5 style)
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
            // In DMC 5, all Judgement Cuts hit the same target location
            // This ensures maximum damage concentration like Vergil's technique
            return targetPosition;
        }

        private void CreateJudgementSlash(IntVec3 position)
        {
            Map map = CasterPawn.Map;
            
            // Increment slash counter for visual effects
            currentSlashCount++;
            
            float effectScale = 2f + (currentSlashCount * 0.5f); // Each slash gets bigger
            
            // Create main slash effect with multiple visual layers
            FleckMaker.ThrowDustPuff(position.ToVector3Shifted(), map, effectScale);
            FleckMaker.ThrowLightningGlow(position.ToVector3Shifted(), map, effectScale * 0.8f);
            
            // Add extra dramatic effect for multiple slashes
            if (currentSlashCount > 1)
            {
                FleckMaker.ThrowDustPuff(position.ToVector3Shifted(), map, effectScale * 0.7f);
                FleckMaker.ThrowLightningGlow(position.ToVector3Shifted(), map, effectScale * 0.5f);
            }

            // Create explosion-like effect with damage radius of 2
            GenExplosion.DoExplosion(
                center: position,
                map: map,
                radius: 2f,
                damType: DamageDefOf.Cut, // Will be overridden by custom damage
                instigator: CasterPawn,
                damAmount: 0, // We'll apply custom damage
                armorPenetration: 0f,
                explosionSound: SoundDefOf.Pawn_Melee_Punch_HitPawn, // Use a valid sound
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
                doVisualEffects: true, // Let explosion show its own effects too
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
                        FleckMaker.ThrowDustPuff(targetPawn.Position.ToVector3Shifted(), map, 1.0f);
                        FleckMaker.ThrowLightningGlow(targetPawn.Position.ToVector3Shifted(), map, 0.8f);
                    }
                }
            }
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = false;
            return 2f; // Show the damage radius
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            // Draw the damage area highlight
            if (target.IsValid)
            {
                GenDraw.DrawFieldEdges(GenRadial.RadialCellsAround(target.Cell, 2f, true).ToList());
            }
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