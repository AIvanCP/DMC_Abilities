using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace DMCAbilities
{
    public static class WeaponDamageUtility
    {
        /// <summary>
        /// Enhanced melee damage calculation with extensive modded weapon support
        /// </summary>
        /// <param name="pawn">The pawn using the weapon</param>
        /// <param name="multiplier">Damage multiplier to apply</param>
        /// <returns>DamageInfo for the attack, or null if no valid melee weapon</returns>
        public static DamageInfo? CalculateMeleeDamage(Pawn pawn, float multiplier = 1f)
        {
            try
            {
                if (pawn?.equipment?.Primary == null)
                    return null;

                ThingWithComps weapon = pawn.equipment.Primary;
                
                // Enhanced weapon validation for better mod compatibility
                var meleeVerb = GetMeleeVerb(weapon);
                if (meleeVerb == null)
                    return null;

                // Enhanced base damage calculation with fallbacks for modded weapons
                float baseDamage = GetWeaponBaseDamage(weapon, meleeVerb);
                if (baseDamage <= 0)
                {
                    // Smart fallback based on weapon type for unknown modded weapons
                    baseDamage = GetFallbackDamageForUnknownWeapon(weapon);
                }

                // Enhanced weapon type bonuses (works with modded weapons)
                if (IsSword(weapon) || IsBladeWeapon(weapon))
                {
                    float swordBonusMultiplier = 1f + (DMCAbilitiesMod.settings?.swordDamageBonus ?? 10f) / 100f;
                    baseDamage *= swordBonusMultiplier;
                }

                // Apply ability multiplier
                baseDamage *= multiplier;

                // Enhanced damage type detection for modded weapons
                DamageDef damageDef = GetWeaponDamageType(weapon, meleeVerb) ?? DamageDefOf.Cut;

                // Create damage info with enhanced compatibility
                return new DamageInfo(
                    damageDef,
                    (int)baseDamage,
                    0.1f, // armor penetration
                    -1f, // angle
                    pawn, // instigator
                    null, // hitPart - let system determine
                    weapon.def // weapon
                );
            }
            catch (System.Exception ex)
            {
                // Graceful fallback if weapon analysis fails with modded weapons
                Log.Warning($"[DMC Abilities] Error calculating melee damage for modded weapon: {ex.Message}");
                return new DamageInfo(DamageDefOf.Cut, (int)(multiplier * 8f), 0.1f, 0f, pawn);
            }
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
            
            // Comprehensive shotgun name patterns (expanded for more modded weapons)
            string[] shotgunPatterns = {
                "shotgun", "pump", "combat_shotgun", "assault_shotgun", "riot_shotgun",
                "scatter", "shot", "boom", "blaster", "buckshot", "gauge", "shell",
                "semi_auto_shotgun", "auto_shotgun", "tactical_shotgun", "hunting_shotgun",
                "sawed_off", "double_barrel", "lever_action_shotgun", "break_action",
                "scattergun", "boomstick", "trench_gun", "coach_gun", "fowler",
                "12gauge", "20gauge", "410gauge", "16gauge", "10gauge",
                "mossberg", "remington", "benelli", "winchester", "spas"
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
                    "shotgun", "scatter", "blaster", "boom", "pump", "gauge", "shell",
                    "buckshot", "riot", "combat_shotgun", "hunting_shotgun", "scattergun",
                    "boomstick", "trench", "coach", "fowler", "tactical_shotgun",
                    "12ga", "20ga", "410ga", "16ga", "10ga"
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

            // Check weapon label/description for shotgun-like names
            string label = weapon.def.label?.ToLower() ?? "";
            foreach (string pattern in shotgunPatterns)
            {
                if (label.Contains(pattern))
                    return true;
            }

            // Check description for shotgun indicators
            string description = weapon.def.description?.ToLower() ?? "";
            string[] descriptionPatterns = { "shotgun", "scatter", "buckshot", "shell", "gauge" };
            foreach (string pattern in descriptionPatterns)
            {
                if (description.Contains(pattern))
                    return true;
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
                            projName.Contains("scatter") || projName.Contains("pellet") ||
                            projName.Contains("shell") || projName.Contains("gauge"))
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a weapon is primarily ranged
        /// </summary>
        public static bool IsRangedWeapon(ThingWithComps weapon)
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

        /// <summary>
        /// Force teleport a pawn to exact destination, bypassing ALL obstacles (for Rapid Slash)
        /// </summary>
        public static bool ForceTeleportPawn(Pawn pawn, IntVec3 destination, bool showEffects = true)
        {
            if (pawn?.Map == null || !destination.InBounds(pawn.Map))
                return false;

            Map map = pawn.Map;
            IntVec3 originalPos = pawn.Position;

            if (showEffects)
            {
                // Create dust effect at origin
                FleckMaker.ThrowDustPuff(originalPos.ToVector3Shifted(), map, 1.5f);
            }

            // Force teleportation to exact position - bypass ALL obstacles
            pawn.Position = destination;
            pawn.Notify_Teleported(false, true);

            if (showEffects)
            {
                // Create effects at destination
                FleckMaker.ThrowDustPuff(destination.ToVector3Shifted(), map, 1.5f);
                FleckMaker.Static(destination, map, FleckDefOf.ExplosionFlash, 1.5f);
                
                // Play teleport sound
                SoundStarter.PlayOneShot(SoundDefOf.Pawn_Melee_Punch_Miss, new TargetInfo(destination, map));
            }

            return true;
        }

        /// <summary>
        /// Gets cells in a 90-degree cone blast based on facing direction
        /// </summary>
        /// <param name="center">Center position of the cone</param>
        /// <param name="facingDirection">Direction the caster is facing (Rot4)</param>
        /// <param name="map">Map reference</param>
        /// <param name="range">Range of the cone blast</param>
        /// <returns>List of cells in the cone area</returns>
        public static List<IntVec3> GetConeBlastCells(IntVec3 center, Rot4 facingDirection, Map map, int range = 3)
        {
            List<IntVec3> coneCells = new List<IntVec3>();
            
            // Get the base direction vector
            IntVec3 baseDirection = facingDirection.FacingCell;
            
            // For each distance from 1 to range
            for (int distance = 1; distance <= range; distance++)
            {
                // Calculate the width of the cone at this distance (90 degrees means it expands)
                int coneWidth = distance; // At distance 1: width 1, distance 2: width 2, etc.
                
                // Get the center cell at this distance
                IntVec3 centerAtDistance = center + (baseDirection * distance);
                
                // Add center cell
                if (centerAtDistance.InBounds(map))
                {
                    coneCells.Add(centerAtDistance);
                }
                
                // Add cells to the left and right of center
                IntVec3 perpendicular = GetPerpendicularVector(baseDirection);
                
                for (int offset = 1; offset <= coneWidth; offset++)
                {
                    // Left side
                    IntVec3 leftCell = centerAtDistance + (perpendicular * offset);
                    if (leftCell.InBounds(map))
                    {
                        coneCells.Add(leftCell);
                    }
                    
                    // Right side  
                    IntVec3 rightCell = centerAtDistance - (perpendicular * offset);
                    if (rightCell.InBounds(map))
                    {
                        coneCells.Add(rightCell);
                    }
                }
            }
            
            return coneCells;
        }

        /// <summary>
        /// Gets a perpendicular vector for cone calculation
        /// </summary>
        private static IntVec3 GetPerpendicularVector(IntVec3 direction)
        {
            // Convert direction to perpendicular for cone width
            if (direction == IntVec3.North) return IntVec3.East;
            if (direction == IntVec3.South) return IntVec3.West;
            if (direction == IntVec3.East) return IntVec3.South;
            if (direction == IntVec3.West) return IntVec3.North;
            return IntVec3.East; // Fallback
        }

        /// <summary>
        /// Enhanced targeting system with comprehensive modded race and faction compatibility
        /// </summary>
        /// <param name="caster">The pawn using the ability</param>
        /// <param name="target">The potential target pawn</param>
        /// <returns>True if should be targeted, false if should be avoided</returns>
        public static bool ShouldTargetPawn(Pawn caster, Pawn target)
        {
            try
            {
                // Basic null and validity checks
                if (caster == null || target == null || target.Dead || target.Downed) return false;
                if (caster == target) return false; // Never target self
                
                // If friendly fire is disabled, perform comprehensive relationship checks
                if (DMCAbilitiesMod.settings?.disableFriendlyFire == true)
                {
                    // Enhanced colonist protection (works with modded human-like races)
                    if (IsColonistOrAlliedPawn(caster, target))
                        return false;
                    
                    // Enhanced faction relationship checks (works with modded factions)
                    if (AreAlliedFactions(caster, target))
                        return false;
                    
                    // Enhanced animal checks (works with modded creatures)
                    if (IsProtectedAnimal(caster, target))
                        return false;
                        
                    // Additional protection for special relationships
                    if (HasSpecialProtection(caster, target))
                        return false;
                }
                
                // Additional checks for unconscious, sleeping, or otherwise non-threatening targets
                if (target.InMentalState == false && target.NonHumanlikeOrWildMan() == false)
                {
                    if (target.Downed || target.InBed() || target.jobs?.curJob?.def?.defName?.Contains("Sleep") == true)
                    {
                        // Only target sleeping/downed enemies, not neutrals
                        if (caster.Faction != null && target.Faction != null && 
                            caster.Faction.HostileTo(target.Faction))
                        {
                            return true; // Hostile sleeping target is valid
                        }
                        return false; // Don't target sleeping neutrals
                    }
                }
                
                return true; // Target is valid for attack
            }
            catch (System.Exception ex)
            {
                // Graceful degradation for mod compatibility
                Log.Warning($"[DMC Abilities] Error in targeting system: {ex.Message}");
                // Conservative fallback - only target if explicitly hostile
                return caster?.Faction?.HostileTo(target?.Faction) == true;
            }
        }
        
        /// <summary>
        /// Enhanced colonist detection for modded races and scenarios
        /// </summary>
        private static bool IsColonistOrAlliedPawn(Pawn caster, Pawn target)
        {
            // Standard colonist checks
            if (target.IsColonist) return true;
            if (caster.IsColonistPlayerControlled && target.IsColonistPlayerControlled) return true;
            
            // Check for player-controlled factions (supports modded scenarios)
            if (caster.Faction?.IsPlayer == true && target.Faction?.IsPlayer == true) return true;
            
            // Check for humanlike allies (covers modded human-like races)
            if (target.RaceProps?.Humanlike == true && target.Faction == caster.Faction) return true;
            
            // Check for guest/prisoner protection
            if (target.GuestStatus == GuestStatus.Guest || target.IsPrisonerOfColony) return true;
            
            return false;
        }
        
        /// <summary>
        /// Enhanced faction relationship checks for modded factions
        /// </summary>
        private static bool AreAlliedFactions(Pawn caster, Pawn target)
        {
            if (caster.Faction == null || target.Faction == null) return false;
            
            // Same faction
            if (caster.Faction == target.Faction) return true;
            
            // Not hostile = allied (covers neutral and friendly)
            if (!caster.Faction.HostileTo(target.Faction)) return true;
            
            // Check for specific alliance relationships (modded factions may use this)
            if (caster.Faction.def?.permanentEnemy == false && 
                target.Faction.def?.permanentEnemy == false)
            {
                // Both are non-permanently-enemy factions, err on side of caution
                var goodwill = caster.Faction.GoodwillWith(target.Faction);
                if (goodwill >= -10) return true; // Near-neutral or better
            }
            
            return false;
        }
        
        /// <summary>
        /// Enhanced animal protection for modded creatures
        /// </summary>
        private static bool IsProtectedAnimal(Pawn caster, Pawn target)
        {
            if (!target.RaceProps?.Animal == true) return false;
            
            // Same faction animals (tamed pets, etc.)
            if (target.Faction == caster.Faction) return true;
            
            // Player-owned animals
            if (target.Faction?.IsPlayer == true) return true;
            
            // Bonded animals (even if different faction)
            if (target.relations?.DirectRelationExists(PawnRelationDefOf.Bond, caster) == true) return true;
            
            // Check for special animal categories that might be protected
            if (target.RaceProps != null)
            {
                // Pack animals - check if they can carry items
                if (target.RaceProps.packAnimal && target.Faction == caster.Faction) return true;
                
                // Trainable animals that are trained (likely pets/working animals)
                if (target.training != null && target.Faction == caster.Faction) return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Additional protection checks for special relationships and modded content
        /// </summary>
        private static bool HasSpecialProtection(Pawn caster, Pawn target)
        {
            try
            {
                // Family relationships
                if (caster.relations?.DirectRelationExists(PawnRelationDefOf.Spouse, target) == true ||
                    caster.relations?.DirectRelationExists(PawnRelationDefOf.Parent, target) == true ||
                    caster.relations?.DirectRelationExists(PawnRelationDefOf.Child, target) == true ||
                    caster.relations?.DirectRelationExists(PawnRelationDefOf.Sibling, target) == true)
                {
                    return true;
                }
                
                // Lovers (Friend relation may not exist in all RimWorld versions)
                if (caster.relations?.DirectRelationExists(PawnRelationDefOf.Lover, target) == true)
                {
                    return true;
                }
                
                // Check for modded relationship types that should be protected
                if (caster.relations?.DirectRelations != null)
                {
                    foreach (var relation in caster.relations.DirectRelations)
                    {
                        if (relation.otherPawn == target && relation.def != null)
                        {
                            // Any positive relationship should be considered protective
                            if (relation.def.opinionOffset > 0)
                                return true;
                        }
                    }
                }
                
                return false;
            }
            catch (System.Exception)
            {
                // If relationship checks fail, err on side of caution
                return false;
            }
        }

        /// <summary>
        /// Calculates skill-based damage for abilities that summon projectiles (Red Hot Night, Heavy Rain, etc.)
        /// Uses the ability name instead of weapon for damage attribution
        /// </summary>
        /// <param name="pawn">The pawn using the ability</param>
        /// <param name="multiplier">Damage multiplier to apply</param>
        /// <param name="abilityName">Name of the ability for damage attribution</param>
        /// <param name="damageDef">Type of damage to deal (default: Burn for fire abilities)</param>
        /// <returns>DamageInfo for the skill-based attack</returns>
        public static DamageInfo CalculateSkillDamage(Pawn pawn, float multiplier = 1f, string abilityName = "DMC Ability", DamageDef damageDef = null)
        {
            // Base damage for summoned abilities (independent of weapon)
            float baseDamage = 15f; // Reasonable base for fire/energy attacks
            
            // Apply pawn shooting skill modifier for ranged abilities like Red Hot Night
            if (pawn?.skills != null)
            {
                int shootingSkill = pawn.skills.GetSkill(SkillDefOf.Shooting)?.Level ?? 0;
                float skillMultiplier = 1f + (shootingSkill * 0.1f); // 10% per skill level
                baseDamage *= skillMultiplier;
            }
            
            // Apply multiplier
            baseDamage *= multiplier;
            
            // Use provided damage type or default to Burn for fire abilities
            DamageDef finalDamageDef = damageDef ?? DamageDefOf.Burn;
            
            // Create damage info with NULL weapon (so it shows ability name)
            return new DamageInfo(
                def: finalDamageDef,
                amount: (int)baseDamage,
                armorPenetration: 0.15f, // Moderate penetration for magical attacks
                angle: 0f,
                instigator: pawn,
                weapon: null, // NULL weapon = shows ability name instead
                category: DamageInfo.SourceCategory.ThingOrUnknown
            );
        }

        /// <summary>
        /// Enhanced weapon compatibility methods for modded weapons
        /// </summary>
        
        private static float GetFallbackDamageForUnknownWeapon(ThingWithComps weapon)
        {
            // Smart fallback based on weapon properties for unknown modded weapons
            if (weapon?.def == null) return 8f;
            
            // Check weapon market value as damage indicator
            float marketValue = weapon.def.BaseMarketValue;
            if (marketValue > 0)
            {
                // Simple min/max without GenMath complexity
                float damageFactor = marketValue / 100f;
                if (damageFactor < 0.1f) damageFactor = 0.1f;
                if (damageFactor > 2.0f) damageFactor = 2.0f;
                return 8f * damageFactor; // Scale base damage by market value
            }
            
            // Check weapon mass as another indicator
            float mass = weapon.def.BaseMass;
            if (mass > 0)
            {
                float massDamage = 8f + (mass * 2f);
                if (massDamage < 5f) massDamage = 5f;
                if (massDamage > 25f) massDamage = 25f;
                return massDamage; // Heavier = more damage
            }
            
            // Check weapon tech level
            if (weapon.def.techLevel >= TechLevel.Industrial)
            {
                return 12f; // Higher tech = more damage
            }
            
            return 8f; // Safe fallback
        }
        
        private static bool IsBladeWeapon(ThingWithComps weapon)
        {
            if (weapon?.def?.defName == null) return false;
            
            string defName = weapon.def.defName.ToLower();
            string[] bladePatterns = {
                "blade", "sword", "katana", "saber", "rapier", "cutlass", "scimitar",
                "machete", "cleaver", "knife", "dagger", "stiletto", "dirk",
                "gladius", "falchion", "broadsword", "longsword", "greatsword",
                "claymore", "flamberge", "zweihander", "nodachi", "wakizashi",
                "tanto", "ninjato", "dao", "jian"
            };
            
            return bladePatterns.Any(pattern => defName.Contains(pattern));
        }

        // DMC dialogue system removed per user request - floating text was too complex
    }
}