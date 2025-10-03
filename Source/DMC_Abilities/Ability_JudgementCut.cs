using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DMCAbilities
{
    public class Verb_JudgementCut : Verb_CastAbility
    {
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

            // Execute immediately without custom warmup
            ExecuteJudgementCut();
            return true;
        }

        private void ExecuteJudgementCut()
        {
            if (CasterPawn == null || CasterPawn.Map == null || !currentTarget.IsValid)
                return;

            // Show Judgement Cut callout
            DMCSpeechUtility.TryShowCallout(CasterPawn, "DMC_JudgementCutActivation", (DMCAbilitiesMod.settings?.calloutChance ?? 75f) * 0.9f);

            IntVec3 targetPosition = currentTarget.Cell;

            // Additional safety check - make sure pawn is still alive and conscious
            if (CasterPawn.Dead || CasterPawn.Downed)
                return;

            // Determine number of slashes
            int slashCount = DetermineSlashCount();
            
            // Create all slashes at the target location
            for (int i = 0; i < slashCount; i++)
            {
                IntVec3 slashPos = GetSlashPosition(i, slashCount, targetPosition);
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

        private IntVec3 GetSlashPosition(int slashIndex, int totalSlashes, IntVec3 targetPos)
        {
            // In DMC 5, all Judgement Cuts hit the same target location
            // This ensures maximum damage concentration like Vergil's technique
            return targetPos;
        }

        private void CreateJudgementSlash(IntVec3 position)
        {
            Map map = CasterPawn.Map;
            
            // Create main slash effect with psycast visual (the one you remember!)
            FleckMaker.Static(position, map, FleckDefOf.PsycastAreaEffect, 3f);
            
            // Play Vergil-style sound - use a slashing/psychic sound
            SoundStarter.PlayOneShot(SoundDefOf.Psycast_Skip_Pulse, new TargetInfo(position, map));

            // Create explosion-like effect with damage radius of 2 (use default explosion sound)
            GenExplosion.DoExplosion(
                center: position,
                map: map,
                radius: 2f,
                damType: DamageDefOf.Cut, // Will be overridden by custom damage
                instigator: CasterPawn,
                damAmount: 0, // We'll apply custom damage
                armorPenetration: 0f,
                explosionSound: SoundDefOf.Pawn_Melee_Punch_HitPawn, // Use punch sound as placeholder
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
                // Target pawns (animals, mechs, humanoids) and turrets, but not other buildings
                if ((thing is Pawn targetPawn && targetPawn != CasterPawn) ||
                    (thing.def.building?.IsTurret == true))
                {
                    // Apply friendly fire protection for pawns
                    if (thing is Pawn pawn && DMCAbilitiesMod.settings?.disableFriendlyFire == true && 
                        !WeaponDamageUtility.ShouldTargetPawn(CasterPawn, pawn))
                    {
                        continue; // Skip friendly targets
                    }
                    
                    // Calculate damage with settings multiplier
                    float multiplier = DMCAbilitiesMod.settings?.judgementCutDamageMultiplier ?? 1.0f;
                    var damageInfo = WeaponDamageUtility.CalculateMeleeDamage(CasterPawn, multiplier);
                    if (damageInfo.HasValue)
                    {
                        // Apply damage
                        thing.TakeDamage(damageInfo.Value);
                        
                        // Create impact effect on each target - use psycast effect
                        FleckMaker.Static(thing.Position, map, FleckDefOf.PsycastAreaEffect, 1.5f);
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
                // Draw clear radius ring showing exact damage area
                GenDraw.DrawRadiusRing(target.Cell, 2f);
                
                // Also draw field edges for additional clarity
                GenDraw.DrawFieldEdges(GenRadial.RadialCellsAround(target.Cell, 2f, true).ToList());
            }
        }

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.ValidateTarget(target, showMessages))
                return false;

            return true;
        }
    }
}