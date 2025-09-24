using System.Collections.Generic;
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

            // Find teleport position adjacent to target
            IntVec3 teleportPos = FindTeleportPosition(caster, target);
            if (!teleportPos.IsValid)
            {
                Messages.Message("DMC_NoValidTeleportPosition".Translate(), 
                    caster, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // Perform safe teleport
            bool teleportSucceeded = WeaponDamageUtility.SafeTeleportPawn(caster, teleportPos, false);
            
            if (teleportSucceeded)
            {
                // Create visual effects
                CreateStingerEffects(caster.Position, target.Position);
            }

            // Apply damage
            ApplyStingerDamage(caster, target);

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
            return 0f;
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

    public class JobDriver_CastStinger : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);
            
            Toil castToil = new Toil
            {
                initAction = delegate
                {
                    Ability ability = job.ability;
                    if (ability != null && ability.verb is Verb_Stinger stingerVerb)
                    {
                        stingerVerb.TryStartCastOn(job.GetTarget(TargetIndex.A), false, true);
                    }
                    else
                    {
                        EndJobWith(JobCondition.Errored);
                    }
                }
            };
            castToil.FailOnDespawnedOrNull(TargetIndex.A);
            yield return castToil;
        }
    }
}