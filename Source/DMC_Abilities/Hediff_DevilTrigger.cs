using RimWorld;
using Verse;
using UnityEngine;
using System.Linq;

namespace DMCAbilities
{
    public class Hediff_DevilTrigger : HediffWithComps
    {
        private const int BaseDuration = 1800; // 30 seconds base duration (30 * 60 ticks)
        
        public float damageMultiplier = 1.5f;
        
        private int ticksSinceLastDamage = 0;
        private int ticksSinceLastKill = 0;

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
                FleckMaker.ThrowDustPuff(this.pawn.Position.ToVector3Shifted(), this.pawn.Map, 1.5f);
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

            // Enhanced regeneration every 60 ticks (1 second)
            if (this.ageTicks % 60 == 0)
            {
                PerformRegeneration(2.0f); // Heal 2 HP per second
            }

            // Visual effects every few seconds
            if (this.ageTicks % 180 == 0) // Every 3 seconds
            {
                FleckMaker.ThrowDustPuff(this.pawn.Position.ToVector3Shifted(), this.pawn.Map, 0.5f);
            }
        }
        
        private void PerformRegeneration(float healAmount)
        {
            if (this.pawn?.health?.hediffSet == null) return;
            
            // Find the most severe injury and heal it
            var injuries = this.pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(h => h.CanHealNaturally() && !h.IsPermanent())
                .OrderByDescending(h => h.Severity)
                .ToList();
            
            if (injuries.Any())
            {
                var injury = injuries.First();
                injury.Heal(healAmount);
                
                // Visual healing effect
                if (this.pawn.Map != null && healAmount > 1.0f)
                {
                    FleckMaker.ThrowDustPuff(this.pawn.Position.ToVector3Shifted(), this.pawn.Map, 0.3f);
                }
            }
        }

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
                return $"Devil Trigger transformation active. Enhanced combat capabilities.\n" +
                       $"Damage multiplier: {damageMultiplier:F1}x\n" +
                       $"Duration remaining: {(BaseDuration - this.ageTicks).ToStringSecondsFromTicks()}";
            }
        }
    }

    public class Hediff_SinDevilTrigger : HediffWithComps
    {
        private const int BaseDuration = 3600; // 60 seconds base duration (60 * 60 ticks)
        
        public float damageMultiplier = 2.0f;
        
        private int ticksSinceLastDamage = 0;
        private int ticksSinceLastKill = 0;

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
                for (int i = 0; i < 8; i++)
                {
                    FleckMaker.ThrowDustPuff(this.pawn.Position.ToVector3Shifted(), this.pawn.Map, 2.0f);
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

            // Ultimate regeneration every 30 ticks (0.5 seconds)
            if (this.ageTicks % 30 == 0)
            {
                PerformUltimateRegeneration(5.0f); // Heal 5 HP per 0.5 seconds
            }

            // More frequent visual effects for Sin Devil Trigger
            if (this.ageTicks % 120 == 0) // Every 2 seconds
            {
                FleckMaker.ThrowDustPuff(this.pawn.Position.ToVector3Shifted(), this.pawn.Map, 1.0f);
            }
        }
        
        private void PerformUltimateRegeneration(float healAmount)
        {
            if (this.pawn?.health?.hediffSet == null) return;
            
            // Heal all injuries simultaneously at accelerated rate
            var injuries = this.pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(h => h.CanHealNaturally())
                .OrderByDescending(h => h.Severity)
                .ToList();
            
            foreach (var injury in injuries.Take(3)) // Heal up to 3 injuries at once
            {
                injury.Heal(healAmount);
            }
            
            // Also heal permanent injuries occasionally
            if (this.ageTicks % 180 == 0) // Every 3 seconds
            {
                var permanentInjuries = this.pawn.health.hediffSet.hediffs
                    .OfType<Hediff_Injury>()
                    .Where(h => h.IsPermanent())
                    .Take(1);
                    
                foreach (var injury in permanentInjuries)
                {
                    injury.Heal(2.0f); // Slowly heal permanent injuries
                }
            }
            
            // Visual healing effect
            if (injuries.Any() && this.pawn.Map != null)
            {
                FleckMaker.ThrowDustPuff(this.pawn.Position.ToVector3Shifted(), this.pawn.Map, 0.6f);
            }
        }

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
                return $"Sin Devil Trigger transformation active. Ultimate demonic power unleashed.\n" +
                       $"Damage multiplier: {damageMultiplier:F1}x\n" +
                       $"Terrain immunity active\n" +
                       $"Duration remaining: {(BaseDuration - this.ageTicks).ToStringSecondsFromTicks()}";
            }
        }
    }
}