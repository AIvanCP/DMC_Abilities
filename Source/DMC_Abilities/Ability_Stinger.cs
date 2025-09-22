using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

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

            // Perform teleport
            TeleportPawn(caster, teleportPos);

            // Create visual effects
            CreateStingerEffects(caster.Position, target.Position);

            // Apply damage
            ApplyStingerDamage(caster, target);

            return true;
        }

        private IntVec3 FindTeleportPosition(Pawn caster, Pawn target)
        {
            Map map = caster.Map;
            IntVec3 targetPos = target.Position;

            // Try adjacent cells around target
            List<IntVec3> adjacentCells = new List<IntVec3>();
            
            foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(target))
            {
                if (cell.InBounds(map) && cell.Standable(map) && 
                    cell.GetFirstPawn(map) == null && caster.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
                {
                    adjacentCells.Add(cell);
                }
            }

            if (adjacentCells.Count == 0)
                return IntVec3.Invalid;

            // Prefer cells that are closer to the caster's current position
            adjacentCells.SortBy(cell => cell.DistanceTo(caster.Position));
            return adjacentCells[0];
        }

        private void TeleportPawn(Pawn pawn, IntVec3 destination)
        {
            // Create dust effect at origin
            FleckMaker.ThrowDustPuff(pawn.Position.ToVector3Shifted(), pawn.Map, 1.5f);

            // Move pawn
            pawn.Position = destination;
            pawn.Notify_Teleported(false, true);

            // Create dust effect at destination
            FleckMaker.ThrowDustPuff(destination.ToVector3Shifted(), pawn.Map, 1.5f);
        }

        private void CreateStingerEffects(IntVec3 originPos, IntVec3 targetPos)
        {
            Map map = CasterPawn.Map;

            // Dust puff at dash location
            FleckMaker.ThrowDustPuff(originPos.ToVector3Shifted(), map, 2f);

            // Impact flash on hit
            FleckMaker.Static(targetPos, map, FleckDefOf.PsycastAreaEffect, 1.5f);
        }

        private void ApplyStingerDamage(Pawn caster, Pawn target)
        {
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

                // Create impact effect
                FleckMaker.Static(target.Position, target.Map, FleckDefOf.MicroSparks, 1f);
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