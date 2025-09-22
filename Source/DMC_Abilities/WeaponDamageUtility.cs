using System.Linq;
using RimWorld;
using Verse;

namespace DMCAbilities
{
    public static class WeaponDamageUtility
    {
        /// <summary>
        /// Calculates melee damage from the equipped weapon, supporting modded weapons
        /// </summary>
        /// <param name="pawn">The pawn using the weapon</param>
        /// <param name="multiplier">Damage multiplier to apply</param>
        /// <returns>DamageInfo for the attack, or null if no valid melee weapon</returns>
        public static DamageInfo? CalculateMeleeDamage(Pawn pawn, float multiplier = 1f)
        {
            if (pawn?.equipment?.Primary == null)
                return null;

            ThingWithComps weapon = pawn.equipment.Primary;
            
            // Check if weapon has any melee verbs
            var meleeVerb = GetMeleeVerb(weapon);
            if (meleeVerb == null)
                return null;

            // Get base damage from the weapon
            float baseDamage = GetWeaponBaseDamage(weapon, meleeVerb);
            if (baseDamage <= 0)
            {
                // Fallback - if we can't get weapon damage, use a small default
                baseDamage = 8f; // Reasonable fallback damage
            }

            // Apply sword bonus (configurable via mod settings)
            if (IsSword(weapon))
            {
                float swordBonusMultiplier = 1f + (DMCAbilitiesMod.settings?.swordDamageBonus ?? 10f) / 100f;
                baseDamage *= swordBonusMultiplier;
            }

            // Apply ability multiplier
            baseDamage *= multiplier;

            // Get damage type from weapon or use Cut as fallback
            DamageDef damageDef = GetWeaponDamageType(weapon, meleeVerb);

            // Create damage info (simplified for RimWorld 1.6 compatibility)
            return new DamageInfo(
                def: damageDef,
                amount: (int)baseDamage,
                armorPenetration: 0.1f, // Use a reasonable default
                angle: 0f,
                instigator: pawn,
                weapon: weapon.def
            );
        }

        /// <summary>
        /// Checks if the pawn has a valid melee weapon equipped
        /// </summary>
        public static bool HasMeleeWeapon(Pawn pawn)
        {
            if (pawn?.equipment?.Primary == null)
                return false;

            ThingWithComps weapon = pawn.equipment.Primary;
            
            // Check if weapon is ranged (has ranged verbs) - if so, reject
            if (IsRangedWeapon(weapon))
                return false;

            return GetMeleeVerb(weapon) != null;
        }

        /// <summary>
        /// Checks if a weapon is primarily ranged
        /// </summary>
        private static bool IsRangedWeapon(ThingWithComps weapon)
        {
            if (weapon?.GetComp<CompEquippable>()?.AllVerbs == null)
                return false;

            // If weapon has any ranged verbs with range > 2, consider it ranged
            var rangedVerbs = weapon.GetComp<CompEquippable>().AllVerbs
                .Where(v => v.verbProps != null && v.verbProps.range > 2f && !v.verbProps.IsMeleeAttack);

            return rangedVerbs.Any();
        }

        /// <summary>
        /// Gets the first melee verb from a weapon
        /// </summary>
        private static Verb GetMeleeVerb(ThingWithComps weapon)
        {
            if (weapon?.GetComp<CompEquippable>()?.AllVerbs == null)
                return null;

            return weapon.GetComp<CompEquippable>().AllVerbs
                .FirstOrDefault(v => v is Verb_MeleeAttack || 
                                   (v.verbProps != null && v.verbProps.range <= 1.42f && v.verbProps.IsMeleeAttack));
        }

        /// <summary>
        /// Gets base damage from weapon tools or verb properties
        /// </summary>
        private static float GetWeaponBaseDamage(ThingWithComps weapon, Verb meleeVerb)
        {
            float damage = 0f;

            // First try to get damage from weapon tools
            if (weapon.def.tools != null && weapon.def.tools.Count > 0)
            {
                // Use the highest damage tool
                damage = weapon.def.tools.Max(tool => tool.power);
            }

            // If no tools or damage is 0, try verb properties
            if (damage <= 0 && meleeVerb?.verbProps != null)
            {
                damage = meleeVerb.verbProps.meleeDamageDef?.defaultDamage ?? 0f;
            }

            // Apply weapon quality multiplier
            if (weapon.TryGetQuality(out QualityCategory quality))
            {
                // Use a simple quality multiplier since GetStatFactor may not exist
                float qualityMultiplier = 1.0f;
                switch (quality)
                {
                    case QualityCategory.Awful: qualityMultiplier = 0.5f; break;
                    case QualityCategory.Poor: qualityMultiplier = 0.75f; break;
                    case QualityCategory.Normal: qualityMultiplier = 1.0f; break;
                    case QualityCategory.Good: qualityMultiplier = 1.15f; break;
                    case QualityCategory.Excellent: qualityMultiplier = 1.35f; break;
                    case QualityCategory.Masterwork: qualityMultiplier = 1.5f; break;
                    case QualityCategory.Legendary: qualityMultiplier = 1.8f; break;
                }
                damage *= qualityMultiplier;
            }

            return damage;
        }

        /// <summary>
        /// Gets the damage type from weapon or verb
        /// </summary>
        private static DamageDef GetWeaponDamageType(ThingWithComps weapon, Verb meleeVerb)
        {
            // Try verb properties first for RimWorld 1.6
            if (meleeVerb?.verbProps?.meleeDamageDef != null)
                return meleeVerb.verbProps.meleeDamageDef;

            // Try to get from weapon tools
            if (weapon.def.tools != null && weapon.def.tools.Count > 0)
            {
                var tool = weapon.def.tools.OrderByDescending(t => t.power).First();
                // In RimWorld 1.6, look at capacities for damage type
                if (tool.capacities != null && tool.capacities.Count > 0)
                {
                    // For most melee weapons, Cut is a reasonable default
                    return DamageDefOf.Cut;
                }
            }

            // Default to Cut damage
            return DamageDefOf.Cut;
        }

        /// <summary>
        /// Checks if a weapon is a sword (for damage bonus)
        /// </summary>
        private static bool IsSword(ThingWithComps weapon)
        {
            if (weapon?.def == null)
                return false;

            string defName = weapon.def.defName.ToLower();
            string label = weapon.def.label.ToLower();

            return defName.Contains("sword") || label.Contains("sword");
        }

        /// <summary>
        /// Gets a message for when no melee weapon is equipped
        /// </summary>
        public static string GetNoMeleeWeaponMessage()
        {
            return "DMC_NoMeleeWeapon".Translate();
        }
    }
}