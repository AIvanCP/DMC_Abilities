using RimWorld;
using Verse;
using UnityEngine;
using System.Linq;
using System;

namespace DMCAbilities
{
    public class Hediff_DevilTrigger : HediffWithComps
    {
        private const int BaseDuration = 1800; // 30 seconds base duration (30 * 60 ticks)
        
        public float damageMultiplier = 1.5f;
        
        private int ticksSinceLastDamage = 0;
        private int ticksSinceLastKill = 0;

        // Make hediff invisible in health tab alerts and severity bars
        public override bool Visible => false;

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            
            // Set duration based on settings
            this.ageTicks = 0;
            
            // Get damage multiplier from settings
            if (DMCAbilitiesMod.settings != null)
            {
                damageMultiplier = DMCAbilitiesMod.settings.devilTriggerDamageMultiplier;
            }
            
            // Add visual effect
            if (this.pawn?.Map != null)
            {
                // Initial transformation effects
                FleckMaker.Static(this.pawn.Position, this.pawn.Map, FleckDefOf.ExplosionFlash, 2.0f);
                FleckMaker.ThrowDustPuff(this.pawn.Position.ToVector3Shifted(), this.pawn.Map, 1.5f);
                
                // Red energy burst for Devil Trigger
                for (int i = 0; i < 8; i++)
                {
                    FleckMaker.ThrowFireGlow(this.pawn.Position.ToVector3Shifted(), this.pawn.Map, 1.0f);
                }
                
                Messages.Message($"{this.pawn.Name.ToStringShort} has activated Devil Trigger!", 
                    this.pawn, MessageTypeDefOf.PositiveEvent);
            }
        }

        public override void Tick()
        {
            base.Tick();
            
            ticksSinceLastDamage++;
            ticksSinceLastKill++;
            
            // Auto-expire after duration
            if (this.ageTicks >= BaseDuration)
            {
                this.pawn.health.RemoveHediff(this);
                return;
            }

            // Regeneration is handled by XML hediff definitions, not code

            // Visual effects every few seconds - red glow for Devil Trigger
            if (this.ageTicks % 180 == 0) // Every 3 seconds
            {
                if (this.pawn?.Map != null)
                {
                    FleckMaker.ThrowDustPuff(this.pawn.Position.ToVector3Shifted(), this.pawn.Map, 0.5f);
                    FleckMaker.ThrowFireGlow(this.pawn.Position.ToVector3Shifted(), this.pawn.Map, 0.8f);
                }
            }
        }
        
        // Regeneration is handled by XML hediff definitions for safety

        public override void PostRemoved()
        {
            base.PostRemoved();
            
            if (this.pawn?.Map != null)
            {
                Messages.Message($"{this.pawn.Name.ToStringShort}'s Devil Trigger has ended.", 
                    this.pawn, MessageTypeDefOf.NeutralEvent);
                
                // End transformation effects
                FleckMaker.ThrowDustPuff(this.pawn.Position.ToVector3Shifted(), this.pawn.Map, 1.0f);
            }
        }

        public void OnDamageDealt()
        {
            ticksSinceLastDamage = 0;
            // Cooldown reduction is handled by harmony patches
        }

        public void OnKill()
        {
            ticksSinceLastKill = 0;
            // Cooldown reduction is handled by harmony patches
        }

        public override string Description
        {
            get
            {
                try
                {
                    if (this.ageTicks < 0 || BaseDuration <= 0)
                    {
                        return "Devil Trigger transformation active. Enhanced combat capabilities.";
                    }
                    
                    int remaining = BaseDuration - this.ageTicks;
                    if (remaining < 0) remaining = 0;
                    
                    float remainingSeconds = remaining / 60f;
                    
                    return "Devil Trigger transformation active. Demonic power enhances all combat abilities.\n\n" +
                           "Combat Bonuses:\n" +
                           "• Melee damage: +50%\n" +
                           "• Ranged damage: +30%\n" +
                           "• Melee hit chance: +20%\n" +
                           "• Dodge chance: +25%\n" +
                           "• Move speed: +2.0\n" +
                           "• Armor (all): +25%\n" +
                           "• Damage resistance: 25%\n" +
                           "• Injury healing: +300%\n\n" +
                           "Duration remaining: " + remainingSeconds.ToString("F1") + "s";
                }
                catch (System.Exception ex)
                {
                    Log.Warning("[DMC] Error in Devil Trigger description: " + ex.Message);
                    return "Devil Trigger transformation active. Enhanced combat capabilities.";
                }
            }
        }

        public override string TipStringExtra
        {
            get
            {
                try
                {
                    return "Demonic transformation providing enhanced combat capabilities.";
                }
                catch (System.Exception ex)
                {
                    Log.Warning("[DMC] Error in Devil Trigger TipStringExtra: " + ex.Message);
                    return "";
                }
            }
        }
    }

    public class Hediff_SinDevilTrigger : HediffWithComps
    {
        private const int BaseDuration = 3600; // 60 seconds base duration (60 * 60 ticks)
        
        public float damageMultiplier = 2.0f;
        
        private int ticksSinceLastDamage = 0;
        private int ticksSinceLastKill = 0;

        // Make hediff invisible in health tab alerts and severity bars
        public override bool Visible => false;

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            
            // Set duration based on settings
            this.ageTicks = 0;
            
            // Get damage multiplier from settings
            if (DMCAbilitiesMod.settings != null)
            {
                damageMultiplier = DMCAbilitiesMod.settings.sinDevilTriggerDamageMultiplier;
            }
            
            // Remove existing Devil Trigger if active
            var existingDT = this.pawn.health.hediffSet.GetFirstHediffOfDef(DMC_HediffDefOf.DMC_DevilTrigger);
            if (existingDT != null)
            {
                this.pawn.health.RemoveHediff(existingDT);
            }
            
            // Add massive visual effect
            if (this.pawn?.Map != null)
            {
                // Ultimate transformation effects - much more dramatic than DT
                FleckMaker.Static(this.pawn.Position, this.pawn.Map, FleckDefOf.ExplosionFlash, 3.0f);
                FleckMaker.Static(this.pawn.Position, this.pawn.Map, FleckDefOf.PsycastAreaEffect, 2.5f);
                
                for (int i = 0; i < 12; i++)
                {
                    FleckMaker.ThrowDustPuff(this.pawn.Position.ToVector3Shifted(), this.pawn.Map, 2.0f);
                }
                
                // Purple/dark energy effects for Sin Devil Trigger
                for (int i = 0; i < 10; i++)
                {
                    FleckMaker.Static(this.pawn.Position, this.pawn.Map, FleckDefOf.PsycastAreaEffect, 1.5f);
                }
                
                Messages.Message($"{this.pawn.Name.ToStringShort} has activated Sin Devil Trigger!", 
                    this.pawn, MessageTypeDefOf.PositiveEvent);
            }
        }

        public override void Tick()
        {
            base.Tick();
            
            ticksSinceLastDamage++;
            ticksSinceLastKill++;
            
            // Auto-expire after duration
            if (this.ageTicks >= BaseDuration)
            {
                this.pawn.health.RemoveHediff(this);
                return;
            }

            // Terrain immunity for Sin Devil Trigger - remove terrain-based movement penalties
            if (this.pawn?.Map != null && this.ageTicks % 60 == 0) // Every second
            {
                var terrainDef = this.pawn.Position.GetTerrain(this.pawn.Map);
                if (terrainDef?.passability == Traversability.Impassable || 
                    (terrainDef?.pathCost > 0 && terrainDef.pathCost > 1))
                {
                    // SDT grants terrain immunity - ignore difficult terrain
                }
            }

            // Regeneration is handled by XML hediff definitions, not code

            // More frequent visual effects for Sin Devil Trigger - purple/dark energy
            if (this.ageTicks % 120 == 0) // Every 2 seconds
            {
                if (this.pawn?.Map != null)
                {
                    FleckMaker.ThrowDustPuff(this.pawn.Position.ToVector3Shifted(), this.pawn.Map, 1.0f);
                    FleckMaker.Static(this.pawn.Position, this.pawn.Map, FleckDefOf.PsycastAreaEffect, 1.2f);
                }
            }
        }
        
        // Regeneration is handled by XML hediff definitions for safety

        public override void PostRemoved()
        {
            base.PostRemoved();
            
            if (this.pawn?.Map != null)
            {
                Messages.Message($"{this.pawn.Name.ToStringShort}'s Sin Devil Trigger has ended.", 
                    this.pawn, MessageTypeDefOf.NeutralEvent);
                
                // Massive end transformation effects
                for (int i = 0; i < 6; i++)
                {
                    FleckMaker.ThrowDustPuff(this.pawn.Position.ToVector3Shifted(), this.pawn.Map, 1.5f);
                }
            }
        }

        public void OnDamageDealt()
        {
            ticksSinceLastDamage = 0;
            // Cooldown reduction is handled by harmony patches
        }

        public void OnKill()
        {
            ticksSinceLastKill = 0;
            // Cooldown reduction is handled by harmony patches
        }

        public override string Description
        {
            get
            {
                try
                {
                    if (this.ageTicks < 0 || BaseDuration <= 0)
                    {
                        return "Sin Devil Trigger transformation active. Ultimate demonic power unleashed.";
                    }
                    
                    int remaining = BaseDuration - this.ageTicks;
                    if (remaining < 0) remaining = 0;
                    
                    float remainingSeconds = remaining / 60f;
                    
                    return "Sin Devil Trigger transformation active. Ultimate demonic power transcending mortal limits.\n\n" +
                           "Ultimate Combat Bonuses:\n" +
                           "• Melee damage: +150%\n" +
                           "• Ranged damage: +100%\n" +
                           "• Melee hit chance: +35%\n" +
                           "• Dodge chance: +50%\n" +
                           "• Move speed: +3.5 (+100%)\n" +
                           "• Armor (all): +40-50%\n" +
                           "• Damage resistance: 50%\n" +
                           "• Injury healing: +500%\n" +
                           "• Mental break resistance: -50%\n\n" +
                           "Immunities: Stun, Paralysis, Toxic buildup, Temperature extremes\n" +
                           "Duration remaining: " + remainingSeconds.ToString("F1") + "s";
                }
                catch (System.Exception ex)
                {
                    Log.Warning("[DMC] Error in Sin Devil Trigger description: " + ex.Message);
                    return "Sin Devil Trigger transformation active. Ultimate demonic power unleashed.";
                }
            }
        }

        public override string TipStringExtra
        {
            get
            {
                try
                {
                    return "Ultimate demonic transformation providing transcendent combat abilities.";
                }
                catch (System.Exception ex)
                {
                    Log.Warning("[DMC] Error in Sin Devil Trigger TipStringExtra: " + ex.Message);
                    return "";
                }
            }
        }
    }
}