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
        private bool hasCalculatedImpact = false;
        protected Pawn casterPawn;

        public void Initialize(Pawn caster, Vector3 target)
        {
            casterPawn = caster;
            targetPosition = target;
        }

        protected override void Tick()
        {
            base.Tick();

            // Calculate random impact position once when projectile is created
            if (!hasCalculatedImpact)
            {
                // Random landing within 1-2 cell radius of target
                Vector2 randomOffset = Rand.InsideUnitCircle * Rand.Range(1f, 2f);
                actualImpactPosition = targetPosition + new Vector3(randomOffset.x, 0, randomOffset.y);
                hasCalculatedImpact = true;
            }
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

            // Calculate weapon-based ranged damage
            var damageInfo = WeaponDamageUtility.CalculateRangedDamage(casterPawn, 1f);
            if (damageInfo.HasValue)
            {
                // Apply main damage
                target.TakeDamage(damageInfo.Value);

                // 10% chance to apply stagger effect
                if (Rand.Chance(0.1f))
                {
                    ApplyStagger(target);
                }

                // Impact sound effect
                if (target.Map != null)
                {
                    SoundStarter.PlayOneShot(SoundDefOf.Pawn_Melee_Punch_HitPawn, new TargetInfo(target.Position, target.Map));
                }
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