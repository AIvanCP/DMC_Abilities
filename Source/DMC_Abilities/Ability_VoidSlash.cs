using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DMCAbilities
{
    public class Verb_VoidSlash : Verb_CastAbility
    {
        private const float ConeAngleDegrees = 75f; // 75-degree cone (between 60-90°)
        private const float MaxRange = 7f; // 7 cell range (between 6-8)
        private const float BaseDamage = 12f; // Increased damage for melee-only ability

        protected override bool TryCastShot()
        {
            // Check if mod and ability are enabled
            if (DMCAbilitiesMod.settings != null && 
                (!DMCAbilitiesMod.settings.modEnabled || !DMCAbilitiesMod.settings.voidSlashEnabled))
            {
                return false;
            }

            // Check for melee weapon requirement
            if (!WeaponDamageUtility.HasMeleeWeapon(CasterPawn))
            {
                Messages.Message(WeaponDamageUtility.GetNoMeleeWeaponMessage(), 
                    CasterPawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            ExecuteVoidSlash();
            return true;
        }

        private void ExecuteVoidSlash()
        {
            Map map = CasterPawn.Map;
            IntVec3 casterPos = CasterPawn.Position;
            Vector3 targetDirection = (currentTarget.Cell - casterPos).ToVector3().normalized;

            // Create visual and audio effects
            CreateVoidSlashEffects(casterPos, map, targetDirection);

            // Find all targets in cone and apply effects
            List<Thing> affectedTargets = GetTargetsInCone(casterPos, targetDirection, map);
            
            foreach (Thing target in affectedTargets)
            {
                // Don't affect caster or destroyed targets
                if (target != CasterPawn && !target.Destroyed)
                {
                    ApplyVoidSlashEffects(target);
                }
            }
        }

        private List<Thing> GetTargetsInCone(IntVec3 origin, Vector3 direction, Map map)
        {
            List<Thing> targetsInCone = new List<Thing>();
            
            for (int i = 1; i <= MaxRange; i++)
            {
                // Calculate the base position along the direction
                Vector3 basePos = origin.ToVector3() + (direction * i);
                
                // Calculate the cone width at this distance
                float coneWidthAtDistance = i * Mathf.Tan(ConeAngleDegrees * Mathf.Deg2Rad * 0.5f) * 2f;
                
                // Check cells in a line perpendicular to the direction
                Vector3 perpendicular = new Vector3(-direction.z, 0, direction.x); // 90-degree rotation
                
                int cellsToCheck = Mathf.CeilToInt(coneWidthAtDistance / 2f);
                for (int j = -cellsToCheck; j <= cellsToCheck; j++)
                {
                    Vector3 checkPos = basePos + (perpendicular * j);
                    IntVec3 cell = checkPos.ToIntVec3();
                    
                    if (cell.InBounds(map))
                    {
                        // Check if cell is within the cone angle
                        Vector3 cellDirection = (cell - origin).ToVector3().normalized;
                        float angle = Vector3.Angle(direction, cellDirection);
                        
                        if (angle <= ConeAngleDegrees * 0.5f)
                        {
                            List<Thing> thingsInCell = map.thingGrid.ThingsListAtFast(cell);
                            // Copy the list to avoid collection modification issues
                            List<Thing> thingsCopy = new List<Thing>(thingsInCell);
                            foreach (Thing thing in thingsCopy)
                            {
                                // Target pawns (animals, mechs, humanoids) and turrets, but not other buildings
                                if (((thing is Pawn pawn && pawn != null) || 
                                     (thing.def.building?.IsTurret == true)) &&
                                    !targetsInCone.Contains(thing))
                                {
                                    targetsInCone.Add(thing);
                                }
                            }
                        }
                    }
                }
            }
            
            return targetsInCone;
        }

        private void ApplyVoidSlashEffects(Thing target)
        {
            // Null safety checks
            if (target == null || target.Destroyed || target.Map == null)
                return;

            // Apply initial slash damage
            DamageInfo slashDamage = new DamageInfo(
                def: DamageDefOf.Cut,
                amount: BaseDamage,
                armorPenetration: 0.1f,
                angle: 0f,
                instigator: CasterPawn
            );
            target.TakeDamage(slashDamage);

            // Apply void slash debuff only to pawns (turrets don't have health hediffs)
            if (target is Pawn pawnTarget && pawnTarget.health?.hediffSet != null)
            {
                Hediff_VoidSlashDebuff voidDebuff = (Hediff_VoidSlashDebuff)HediffMaker.MakeHediff(
                    DMC_HediffDefOf.DMC_VoidSlashDebuff, pawnTarget);
                pawnTarget.health.AddHediff(voidDebuff);
            }

            // Visual effect on target with null safety
            if (target.Map != null)
            {
                FleckMaker.Static(target.Position, target.Map, FleckDefOf.PsycastAreaEffect, 1.2f);
                
                // Play hit sound
                SoundStarter.PlayOneShot(SoundDefOf.Pawn_Melee_Punch_HitPawn, 
                    new TargetInfo(target.Position, target.Map));
            }
        }

        private void CreateVoidSlashEffects(IntVec3 origin, Map map, Vector3 direction)
        {
            // Create wave effect along the cone
            for (int i = 1; i <= MaxRange; i += 2) // Every 2 cells for performance
            {
                Vector3 effectPos = origin.ToVector3() + (direction * i);
                IntVec3 effectCell = effectPos.ToIntVec3();
                
                if (effectCell.InBounds(map))
                {
                    // Dark purple psychic wave effect
                    FleckMaker.Static(effectCell, map, FleckDefOf.PsycastAreaEffect, 2.0f);
                    
                    // Add some side effects for cone visualization
                    if (i <= 8) // Only for closer range to avoid too many effects
                    {
                        Vector3 perpendicular = new Vector3(-direction.z, 0, direction.x);
                        float coneWidth = i * 0.3f; // Approximate cone width
                        
                        FleckMaker.Static((effectPos + perpendicular * coneWidth).ToIntVec3(), 
                            map, FleckDefOf.PsycastAreaEffect, 1.5f);
                        FleckMaker.Static((effectPos - perpendicular * coneWidth).ToIntVec3(), 
                            map, FleckDefOf.PsycastAreaEffect, 1.5f);
                    }
                }
            }

            // Play void slash sound
            SoundStarter.PlayOneShot(SoundDefOf.Psycast_Skip_Pulse, 
                new TargetInfo(origin, map));
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = false;
            return MaxRange; // Show max range radius during targeting
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            // Draw the cone AoE preview in front of the caster
            if (CasterPawn != null && CasterPawn.Position.IsValid && target.IsValid)
            {
                IntVec3 casterPos = CasterPawn.Position;
                Vector3 targetDirection = (target.Cell - casterPos).ToVector3().normalized;
                
                // Calculate cone cells
                List<IntVec3> coneCells = new List<IntVec3>();
                
                for (int range = 1; range <= MaxRange; range++)
                {
                    // Calculate the base position along the direction
                    Vector3 basePos = casterPos.ToVector3() + (targetDirection * range);
                    
                    // Calculate the cone width at this distance
                    float coneWidthAtDistance = range * Mathf.Tan(ConeAngleDegrees * Mathf.Deg2Rad * 0.5f) * 2f;
                    
                    // Check cells in a line perpendicular to the direction
                    Vector3 perpendicular = new Vector3(-targetDirection.z, 0, targetDirection.x); // 90-degree rotation
                    
                    int cellsToCheck = Mathf.CeilToInt(coneWidthAtDistance / 2f);
                    for (int j = -cellsToCheck; j <= cellsToCheck; j++)
                    {
                        Vector3 checkPos = basePos + (perpendicular * j);
                        IntVec3 cell = checkPos.ToIntVec3();
                        
                        if (cell.InBounds(CasterPawn.Map))
                        {
                            // Check if cell is within the cone angle
                            Vector3 cellDirection = (cell - casterPos).ToVector3().normalized;
                            float angle = Vector3.Angle(targetDirection, cellDirection);
                            
                            if (angle <= ConeAngleDegrees * 0.5f && !coneCells.Contains(cell))
                            {
                                coneCells.Add(cell);
                            }
                        }
                    }
                }
                
                // Draw the cone preview
                if (coneCells.Count > 0)
                {
                    GenDraw.DrawFieldEdges(coneCells);
                }
                
                // Also draw a radius ring at max range for better visualization
                Vector3 maxRangePos = casterPos.ToVector3() + (targetDirection * MaxRange);
                IntVec3 maxRangeCell = maxRangePos.ToIntVec3();
                if (maxRangeCell.InBounds(CasterPawn.Map))
                {
                    GenDraw.DrawRadiusRing(maxRangeCell, 1f);
                }
            }
        }
    }
}