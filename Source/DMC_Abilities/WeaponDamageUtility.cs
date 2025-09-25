using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

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
        /// Calculates ranged damage from the equipped weapon (for Gun Stinger)
        /// </summary>
        /// <param name="pawn">The pawn using the weapon</param>
        /// <param name="multiplier">Damage multiplier to apply</param>
        /// <returns>DamageInfo for the ranged attack, or null if no valid ranged weapon</returns>
        public static DamageInfo? CalculateRangedDamage(Pawn pawn, float multiplier = 1f)
        {
            if (pawn?.equipment?.Primary == null)
                return null;

            ThingWithComps weapon = pawn.equipment.Primary;
            
            // Get ranged verb from the weapon
            var rangedVerb = GetRangedVerb(weapon);
            if (rangedVerb == null)
                return null;

            // Get base damage from the weapon's ranged verb
            float baseDamage = GetWeaponBaseDamage(weapon, rangedVerb);
            if (baseDamage <= 0)
            {
                // Fallback for shotguns
                baseDamage = 12f; // Reasonable shotgun damage
            }

            // Apply multiplier
            baseDamage *= multiplier;

            // Get damage type from ranged weapon projectile
            DamageDef damageDef = GetRangedWeaponDamageType(weapon, rangedVerb) ?? DamageDefOf.Bullet;

            // Create damage info
            return new DamageInfo(
                def: damageDef,
                amount: (int)baseDamage,
                armorPenetration: 0.2f, // Higher penetration for ranged
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
        /// Checks if the pawn has a shotgun-type weapon equipped
        /// Enhanced detection for modded weapons
        /// </summary>
        public static bool HasShotgunWeapon(Pawn pawn)
        {
            if (pawn?.equipment?.Primary == null)
                return false;

            ThingWithComps weapon = pawn.equipment.Primary;
            
            // Check weapon def name for shotgun indicators (expanded patterns)
            string defName = weapon.def.defName.ToLower();
            
            // Comprehensive shotgun name patterns
            string[] shotgunPatterns = {
                "shotgun", "pump", "combat_shotgun", "assault_shotgun", "riot_shotgun",
                "scatter", "shot", "boom", "blaster", "buckshot", "gauge", 
                "semi_auto_shotgun", "auto_shotgun", "tactical_shotgun", "hunting_shotgun",
                "sawed_off", "double_barrel", "lever_action_shotgun", "break_action"
            };
            
            foreach (string pattern in shotgunPatterns)
            {
                if (defName.Contains(pattern))
                    return true;
            }

            // Check weapon categories if available
            if (weapon.def.weaponClasses != null)
            {
                foreach (var weaponClass in weapon.def.weaponClasses)
                {
                    string className = weaponClass.defName.ToLower();
                    if (className.Contains("shotgun") || className.Contains("scatter") || 
                        className.Contains("boom") || className.Contains("blaster"))
                        return true;
                }
            }

            // Check by weapon tags (expanded)
            if (weapon.def.weaponTags != null)
            {
                string[] shotgunTags = {
                    "shotgun", "scatter", "blaster", "boom", "pump", "gauge",
                    "buckshot", "riot", "combat_shotgun", "hunting_shotgun"
                };
                
                foreach (var tag in weapon.def.weaponTags)
                {
                    string tagLower = tag.ToLower();
                    foreach (string shotgunTag in shotgunTags)
                    {
                        if (tagLower.Contains(shotgunTag))
                            return true;
                    }
                }
            }

            // Check verb properties for shotgun-like characteristics
            // Shotguns typically have shorter range and spread/burst patterns
            var rangedVerb = GetRangedVerb(weapon);
            if (rangedVerb != null)
            {
                // Check if it has burst characteristics (many shotguns fire multiple projectiles)
                if (rangedVerb.verbProps != null)
                {
                    // Some shotguns might have burstShotCount > 1 or specific projectile types
                    if (rangedVerb.verbProps.burstShotCount > 3 && rangedVerb.verbProps.range < 25)
                        return true;
                        
                    // Check projectile for shotgun-like properties
                    if (rangedVerb.verbProps.defaultProjectile != null)
                    {
                        string projName = rangedVerb.verbProps.defaultProjectile.defName.ToLower();
                        if (projName.Contains("shotgun") || projName.Contains("buckshot") || 
                            projName.Contains("scatter") || projName.Contains("pellet"))
                            return true;
                    }
                }
            }

            return false;
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
        /// Gets the first ranged verb from a weapon
        /// </summary>
        private static Verb GetRangedVerb(ThingWithComps weapon)
        {
            if (weapon?.GetComp<CompEquippable>()?.AllVerbs == null)
                return null;

            return weapon.GetComp<CompEquippable>().AllVerbs
                .FirstOrDefault(v => v.verbProps != null && v.verbProps.range > 2f && !v.verbProps.IsMeleeAttack);
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
        /// Gets damage type from ranged weapon's projectile
        /// </summary>
        private static DamageDef GetRangedWeaponDamageType(ThingWithComps weapon, Verb rangedVerb)
        {
            // Try to get from verb's projectile
            if (rangedVerb?.verbProps?.defaultProjectile?.projectile?.damageDef != null)
                return rangedVerb.verbProps.defaultProjectile.projectile.damageDef;
            
            // Try direct access to projectile damage
            if (rangedVerb?.verbProps?.defaultProjectile?.GetStatValueAbstract(StatDefOf.RangedWeapon_DamageMultiplier) != null)
            {
                // For most ranged weapons, the projectile defines the damage type
                var projectile = rangedVerb.verbProps.defaultProjectile;
                if (projectile?.projectile?.damageDef != null)
                    return projectile.projectile.damageDef;
            }

            return null;
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

        /// <summary>
        /// Gets a message for when no shotgun weapon is equipped
        /// </summary>
        public static string GetNoShotgunWeaponMessage()
        {
            return "Requires a shotgun-type weapon equipped.";
        }

        /// <summary>
        /// Checks if the pawn has a pistol/revolver-type weapon equipped
        /// Enhanced detection for modded weapons with comprehensive pattern matching
        /// </summary>
        public static bool HasPistolWeapon(Pawn pawn)
        {
            if (pawn?.equipment?.Primary == null)
                return false;

            ThingWithComps weapon = pawn.equipment.Primary;
            
            // Check weapon def name for pistol indicators (expanded for modded weapons)
            string defName = weapon.def.defName.ToLower();
            string label = weapon.def.label?.ToLower() ?? "";
            
            // Comprehensive pistol/revolver name patterns including common modded weapon names
            string[] pistolPatterns = {
                // Basic types
                "pistol", "revolver", "handgun", "sidearm", "magnum", 
                // Real-world brands/models
                "colt", "beretta", "glock", "luger", "mauser", "walther", "sig", "smith", "wesson",
                "desert_eagle", "deagle", "m1911", "p90", "uzi", "mac10", "skorpion",
                // Weapon type variations
                "auto_pistol", "heavy_pistol", "machine_pistol", "bolt_pistol", "stub_pistol",
                "laspistol", "plasma_pistol", "charge_pistol", "needle_pistol", "flintlock_pistol",
                // Sci-fi/fantasy variations common in mods
                "ray_pistol", "laser_pistol", "pulse_pistol", "energy_pistol", "quantum_pistol",
                "arc_pistol", "ion_pistol", "photon_pistol", "sonic_pistol", "phase_pistol",
                // Size indicators that suggest pistol
                "compact", "concealed", "pocket", "mini", "micro", "sub_compact"
            };
            
            foreach (string pattern in pistolPatterns)
            {
                if (defName.Contains(pattern) || label.Contains(pattern))
                    return true;
            }

            // Check weapon categories if available
            if (weapon.def.weaponClasses != null)
            {
                foreach (var weaponClass in weapon.def.weaponClasses)
                {
                    string className = weaponClass.defName.ToLower();
                    if (className.Contains("pistol") || className.Contains("revolver") || 
                        className.Contains("handgun") || className.Contains("sidearm") ||
                        className.Contains("compact") || className.Contains("concealed"))
                        return true;
                }
            }

            // Check by weapon tags (expanded for modded weapons)
            if (weapon.def.weaponTags != null)
            {
                string[] pistolTags = {
                    // Standard tags
                    "pistol", "revolver", "handgun", "sidearm", "magnum", 
                    "autopistol", "heavy_pistol", "machine_pistol", "stub_pistol",
                    // Modded weapon common tags
                    "compact", "concealed", "pocket", "personal", "backup",
                    "laser_pistol", "plasma_pistol", "energy_pistol", "pulse_pistol",
                    // Size/type indicators
                    "small_arms", "sidearm", "secondary", "holdout"
                };
                
                foreach (var tag in weapon.def.weaponTags)
                {
                    string tagLower = tag.ToLower();
                    foreach (string pistolTag in pistolTags)
                    {
                        if (tagLower.Contains(pistolTag))
                            return true;
                    }
                }
            }

            // Check verb properties for pistol-like characteristics
            // Pistols typically have moderate range (6-35) and single-shot or small burst
            var rangedVerb = GetRangedVerb(weapon);
            if (rangedVerb != null && rangedVerb.verbProps != null)
            {
                float range = rangedVerb.verbProps.range;
                int burstCount = rangedVerb.verbProps.burstShotCount;
                float warmupTime = rangedVerb.verbProps.warmupTime;
                
                // Pistol characteristics: moderate range, low burst count, quick warmup
                if (range >= 6f && range <= 35f && burstCount <= 4 && warmupTime <= 1.5f)
                {
                    // Additional checks for projectile properties
                    if (rangedVerb.verbProps.defaultProjectile != null)
                    {
                        string projName = rangedVerb.verbProps.defaultProjectile.defName.ToLower();
                        string projLabel = rangedVerb.verbProps.defaultProjectile.label?.ToLower() ?? "";
                        
                        // Check for pistol-related projectile names
                        if (projName.Contains("pistol") || projName.Contains("revolver") || 
                            projName.Contains("handgun") || projName.Contains("compact") ||
                            projLabel.Contains("pistol") || projLabel.Contains("revolver"))
                            return true;
                            
                        // Generic bullet projectiles with pistol-like characteristics are likely pistols
                        if ((projName.Contains("bullet") || projLabel.Contains("bullet")) &&
                            range <= 30f && burstCount <= 3)
                            return true;
                    }
                    
                    // If weapon has pistol-like stats but no clear projectile indicators,
                    // use more restrictive criteria
                    if (range >= 8f && range <= 25f && burstCount <= 2 && warmupTime <= 1.0f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets a message for when no pistol weapon is equipped
        /// </summary>
        public static string GetNoPistolWeaponMessage()
        {
            return "Requires a pistol or revolver equipped.";
        }

        /// <summary>
        /// Finds a safe landing position near the target for teleportation abilities
        /// </summary>
        /// <param name="targetCell">Preferred landing cell</param>
        /// <param name="map">Map to search on</param>
        /// <param name="pawn">Pawn that will be teleporting (for forbidden checks)</param>
        /// <param name="maxRadius">Maximum search radius (default 5)</param>
        /// <returns>Safe landing position or IntVec3.Invalid if none found</returns>
        public static IntVec3 FindSafeTeleportPosition(IntVec3 targetCell, Map map, Pawn pawn, int maxRadius = 5)
        {
            if (map == null || pawn == null)
                return IntVec3.Invalid;

            // Try the exact target first
            if (IsSafeTeleportCell(targetCell, map, pawn))
            {
                return targetCell;
            }

            // Try adjacent cells
            foreach (IntVec3 cell in GenAdjFast.AdjacentCells8Way(targetCell))
            {
                if (IsSafeTeleportCell(cell, map, pawn))
                {
                    return cell;
                }
            }

            // Try wider radius search
            for (int radius = 2; radius <= maxRadius; radius++)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(targetCell, radius, false))
                {
                    if (IsSafeTeleportCell(cell, map, pawn))
                    {
                        return cell;
                    }
                }
            }

            // Last resort: use CellFinder for any walkable cell near target
            IntVec3 fallbackCell;
            if (CellFinder.TryFindRandomCellNear(targetCell, map, maxRadius + 3, 
                (IntVec3 c) => IsSafeTeleportCell(c, map, pawn), out fallbackCell))
            {
                return fallbackCell;
            }

            return IntVec3.Invalid;
        }

        /// <summary>
        /// Checks if a cell is safe for teleportation (no hazards, passable, not occupied)
        /// </summary>
        /// <param name="cell">Cell to check</param>
        /// <param name="map">Map the cell is on</param>
        /// <param name="pawn">Pawn that will be teleporting</param>
        /// <returns>True if cell is safe for teleportation</returns>
        public static bool IsSafeTeleportCell(IntVec3 cell, Map map, Pawn pawn)
        {
            if (map == null || pawn == null)
                return false;

            // Basic bounds check
            if (!cell.InBounds(map))
                return false;

            // Check if cell is standable (combines walkability, passability, and terrain checks)
            if (!cell.Standable(map))
                return false;

            // Check for existing pawns
            if (cell.GetFirstPawn(map) != null)
                return false;

            // Check terrain safety - avoid dangerous terrain
            TerrainDef terrain = cell.GetTerrain(map);
            if (terrain != null)
            {
                // Avoid lava, deep water, and other impassable/dangerous terrain
                if (terrain.passability == Traversability.Impassable)
                    return false;
                
                // Avoid deep water specifically (high path cost usually indicates deep water)
                if (terrain.HasTag("Water") && terrain.pathCost >= 50)
                    return false;
                
                // Avoid lava and other burning terrain
                if (terrain.HasTag("Lava") || terrain.burnedDef != null)
                    return false;
                
                // Avoid bridgeable terrain (usually water/chasms that need bridges)
                if (terrain.bridge == true)
                    return false;
                
                // Additional dangerous terrain checks
                if (terrain.defName?.Contains("Lava") == true || 
                    terrain.defName?.Contains("WaterDeep") == true ||
                    terrain.defName?.Contains("Marsh") == true)
                    return false;
            }

            // Check for impassable buildings/edifices (walls, etc.)
            Building edifice = cell.GetEdifice(map);
            if (edifice != null && edifice.def.passability == Traversability.Impassable)
                return false;

            // Check for doors - avoid landing on closed doors
            Building_Door door = cell.GetDoor(map);
            if (door != null && !door.Open)
                return false;

            // Check if cell is forbidden for this pawn
            if (cell.IsForbidden(pawn))
                return false;

            // Additional safety: avoid cells with fire or other hazards
            var thingsInCell = cell.GetThingList(map);
            if (thingsInCell != null)
            {
                foreach (Thing thing in thingsInCell)
                {
                    // Avoid fire
                    if (thing.def == ThingDefOf.Fire)
                        return false;
                    
                    // Avoid other impassable things
                    if (thing.def.passability == Traversability.Impassable)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Safely teleports a pawn to a destination with proper effects and safety checks
        /// </summary>
        /// <param name="pawn">Pawn to teleport</param>
        /// <param name="destination">Desired destination (will search for safe nearby cell)</param>
        /// <param name="showEffects">Whether to show visual and audio effects</param>
        /// <returns>True if teleport succeeded, false if no safe destination found</returns>
        public static bool SafeTeleportPawn(Pawn pawn, IntVec3 destination, bool showEffects = true)
        {
            if (pawn?.Map == null)
                return false;

            Map map = pawn.Map;
            IntVec3 originalPos = pawn.Position;
            
            // Find safe landing position
            IntVec3 safeDestination = FindSafeTeleportPosition(destination, map, pawn);
            
            if (safeDestination == IntVec3.Invalid)
            {
                // No safe destination found
                if (showEffects)
                {
                    Messages.Message($"{pawn.LabelShort}: Cannot teleport - no safe ground nearby (avoid deep water, lava, walls).", 
                        pawn, MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }

            if (showEffects)
            {
                // Create dust effect at origin
                FleckMaker.ThrowDustPuff(originalPos.ToVector3Shifted(), map, 1.5f);
            }

            // Perform teleportation
            pawn.Position = safeDestination;
            pawn.Notify_Teleported(false, true);

            if (showEffects)
            {
                // Create effects at destination
                FleckMaker.ThrowDustPuff(safeDestination.ToVector3Shifted(), map, 1.5f);
                FleckMaker.Static(safeDestination, map, FleckDefOf.ExplosionFlash, 1.5f);
                
                // Play teleport sound
                SoundStarter.PlayOneShot(SoundDefOf.Pawn_Melee_Punch_Miss, new TargetInfo(safeDestination, map));
            }

            return true;
        }
    }
}