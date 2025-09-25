using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DMCAbilities
{
    public class Projectile_RainBullet : Projectile
    {
        private Vector3 targetPosition;
        private Vector3 actualImpactPosition;
        protected Pawn casterPawn;

        public void Initialize(Pawn caster, Vector3 target)
        {
            casterPawn = caster;
            targetPosition = target;
            
            // Calculate impact position immediately when initialized
            Vector2 randomOffset = Rand.InsideUnitCircle * Rand.Range(0.5f, 1.5f);
            actualImpactPosition = targetPosition + new Vector3(randomOffset.x, 0, randomOffset.y);
        }

        protected override void Tick()
        {
            base.Tick();
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            IntVec3 impactCell = actualImpactPosition.ToIntVec3();

            if (map == null)
                return;

            // Ensure impact cell is valid
            if (!impactCell.InBounds(map))
                impactCell = Position;

            // Create impact effects
            CreateImpactEffects(impactCell, map);

            // Find pawn at impact location
            Pawn targetPawn = impactCell.GetFirstPawn(map);
            if (targetPawn != null && targetPawn != casterPawn)
            {
                ApplyRainBulletDamage(targetPawn);
            }

            base.Impact(hitThing, blockedByShield);
        }

        private void ApplyRainBulletDamage(Pawn target)
        {
            if (target == null || target.Dead || casterPawn == null)
                return;

            // Calculate weapon-based ranged damage - each bullet should cause full damage
            var damageInfo = WeaponDamageUtility.CalculateRangedDamage(casterPawn, 1f);
            if (damageInfo.HasValue)
            {
                var gunDamage = damageInfo.Value;

                // Each bullet applies full damage independently (continuous damage)
                target.TakeDamage(gunDamage);

                // 10% chance to apply stagger effect per bullet
                if (Rand.Chance(0.1f))
                {
                    ApplyStagger(target);
                }

                // Impact sound effect - use gunshot sound
                if (target.Map != null)
                {
                    SoundStarter.PlayOneShot(SoundDefOf.BulletImpact_Ground, new TargetInfo(target.Position, target.Map));
                }

                Log.Message($"Rain Bullet: Applied {gunDamage.Amount} damage to {target.Label}");
            }
        }

        private void ApplyStagger(Pawn target)
        {
            if (target?.health == null || target.Dead)
                return;

            // Apply stagger hediff - will be defined in XML later
            HediffDef staggerDef = DefDatabase<HediffDef>.GetNamedSilentFail("DMC_Stagger");
            if (staggerDef != null)
            {
                Hediff_Stagger staggerHediff = (Hediff_Stagger)HediffMaker.MakeHediff(staggerDef, target);
                target.health.AddHediff(staggerHediff);
            }
        }

        private void CreateImpactEffects(IntVec3 position, Map map)
        {
            // Bullet impact visual effect
            FleckMaker.Static(position, map, FleckDefOf.ShotFlash, 0.8f);
            
            // Small dust cloud
            FleckMaker.ThrowDustPuffThick(position.ToVector3Shifted(), map, 0.5f, Color.gray);

            // Impact sound
            SoundStarter.PlayOneShot(SoundDefOf.Pawn_Melee_Punch_HitPawn, new TargetInfo(position, map));
        }
    }
}