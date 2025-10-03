using RimWorld;
using UnityEngine;
using Verse;

namespace DMCAbilities
{
    public class DMCAbilitiesMod : Mod
    {
        public static DMCAbilitiesSettings settings;
        private static Vector2 scrollPosition = Vector2.zero;
        private const float ScrollViewHeight = 1100f; // Total content height (increased for all new settings)
        
        public DMCAbilitiesMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<DMCAbilitiesSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // Title
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, 0, inRect.width, 35f), "DMC Abilities Settings");
            Text.Font = GameFont.Small;
            
            // Create scrollable area
            Rect scrollViewRect = new Rect(0, 40f, inRect.width, inRect.height - 40f);
            Rect scrollContentRect = new Rect(0, 0, inRect.width - 20f, ScrollViewHeight);
            
            Widgets.BeginScrollView(scrollViewRect, ref scrollPosition, scrollContentRect, true);
            
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(scrollContentRect);
            
            // === MAIN SETTINGS ===
            DrawSectionHeader(listingStandard, "Main Settings");
            
            listingStandard.CheckboxLabeled("Enable mod (requires restart)", ref settings.modEnabled, 
                "Toggle the entire mod on/off. Requires game restart to take effect.");
            
            listingStandard.CheckboxLabeled("Disable friendly fire", ref settings.disableFriendlyFire,
                "When enabled, abilities won't damage allied pawns, colonists, or tamed animals.");
            
            // === ABILITY TOGGLES ===
            DrawSectionHeader(listingStandard, "Ability Toggles");
            
            CreateAbilityToggle(listingStandard, "Stinger", ref settings.stingerEnabled, 
                "Lightning-fast dash attack with melee weapons");
            CreateAbilityToggle(listingStandard, "Judgement Cut", ref settings.judgementCutEnabled,
                "Ranged dimensional slash with melee weapons");
            CreateAbilityToggle(listingStandard, "Drive", ref settings.driveEnabled,
                "Vertical energy slash that creates projectiles");
            CreateAbilityToggle(listingStandard, "Void Slash", ref settings.voidSlashEnabled,
                "Cone-shaped melee attack with debuffing effects");
            CreateAbilityToggle(listingStandard, "Gun Stinger", ref settings.gunStingerEnabled,
                "Teleport behind target and shotgun blast (shotguns only)");
            CreateAbilityToggle(listingStandard, "Heavy Rain", ref settings.heavyRainEnabled,
                "Rain of spectral swords over large area");
            CreateAbilityToggle(listingStandard, "Rain Bullet", ref settings.rainBulletEnabled,
                "Aerial leap with continuous shooting (pistols/revolvers)");
            CreateAbilityToggle(listingStandard, "Rapid Slash", ref settings.rapidSlashEnabled,
                "Dash forward slashing everything in path");
            CreateAbilityToggle(listingStandard, "Red Hot Night", ref settings.redHotNightEnabled,
                "Devastating orb rain attack (ranged weapons only)");
            CreateAbilityToggle(listingStandard, "Devil Trigger", ref settings.devilTriggerEnabled,
                "Transform to enhance combat abilities (long cooldown)");
            CreateAbilityToggle(listingStandard, "Sin Devil Trigger", ref settings.sinDevilTriggerEnabled,
                "Ultimate transformation with terrain immunity (very long cooldown)");
            
            // === DAMAGE MULTIPLIERS ===
            DrawSectionHeader(listingStandard, "Damage Multipliers");
            
            CreateSliderSetting(listingStandard, "Stinger damage", ref settings.stingerDamageMultiplier, 
                0.5f, 3.0f, "x", "Damage multiplier for Stinger dash attacks");
            CreateSliderSetting(listingStandard, "Judgement Cut damage", ref settings.judgementCutDamageMultiplier, 
                0.5f, 3.0f, "x", "Damage multiplier for Judgement Cut dimensional slashes");
            CreateSliderSetting(listingStandard, "Drive damage", ref settings.driveDamageMultiplier, 
                0.5f, 3.0f, "x", "Damage multiplier for Drive projectiles");
            CreateSliderSetting(listingStandard, "Void Slash damage", ref settings.voidSlashDamageMultiplier, 
                0.5f, 3.0f, "x", "Damage multiplier for Void Slash cone attacks");
            CreateSliderSetting(listingStandard, "Gun Stinger damage", ref settings.gunStingerDamageMultiplier, 
                0.5f, 3.0f, "x", "Damage multiplier for Gun Stinger shotgun blasts");
            CreateSliderSetting(listingStandard, "Heavy Rain damage", ref settings.heavyRainDamageMultiplier, 
                0.5f, 3.0f, "x", "Damage multiplier for Heavy Rain spectral swords");
            CreateSliderSetting(listingStandard, "Rain Bullet damage", ref settings.rainBulletDamageMultiplier, 
                0.5f, 3.0f, "x", "Damage multiplier for Rain Bullet pistol shots");
            CreateSliderSetting(listingStandard, "Rapid Slash damage", ref settings.rapidSlashDamageMultiplier, 
                0.5f, 3.0f, "x", "Damage multiplier for Rapid Slash dash attacks");
            CreateSliderSetting(listingStandard, "Red Hot Night damage", ref settings.redHotNightDamageMultiplier, 
                0.5f, 3.0f, "x", "Damage multiplier for Red Hot Night orb explosions");
            CreateSliderSetting(listingStandard, "Devil Trigger damage", ref settings.devilTriggerDamageMultiplier, 
                1.0f, 4.0f, "x", "Damage multiplier during Devil Trigger transformation");
            CreateSliderSetting(listingStandard, "Sin Devil Trigger damage", ref settings.sinDevilTriggerDamageMultiplier, 
                1.0f, 5.0f, "x", "Damage multiplier during Sin Devil Trigger transformation");
            CreateSliderSetting(listingStandard, "Sword damage bonus", ref settings.swordDamageBonus, 
                0f, 50f, "%", "Extra damage bonus when using sword weapons");
            
            // === PERFORMANCE SETTINGS ===
            DrawSectionHeader(listingStandard, "Performance Settings");
            
            listingStandard.Label($"Max Red Hot Night orbs: {settings.maxRedHotOrbs}");
            settings.maxRedHotOrbs = (int)listingStandard.Slider(settings.maxRedHotOrbs, 5, 50);
            listingStandard.Gap(5f);
            
            // === SKILLBOOK TRADER CHANCES ===
            DrawSectionHeader(listingStandard, "Skillbook Trader Chances");
            Text.Font = GameFont.Tiny;
            listingStandard.Label("Adjust how often skillbooks appear in trader inventories:");
            Text.Font = GameFont.Small;
            listingStandard.Gap(5f);
            
            CreateSliderSetting(listingStandard, "Stinger book", ref settings.stingerTradeChance, 
                0f, 20f, "%", "Chance for Stinger skillbook to appear in trader stock");
            CreateSliderSetting(listingStandard, "Judgement Cut book", ref settings.judgementCutTradeChance, 
                0f, 15f, "%", "Chance for Judgement Cut skillbook to appear in trader stock");
            CreateSliderSetting(listingStandard, "Drive book", ref settings.driveTradeChance, 
                0f, 15f, "%", "Chance for Drive skillbook to appear in trader stock");
            CreateSliderSetting(listingStandard, "Void Slash book", ref settings.voidSlashTradeChance, 
                0f, 15f, "%", "Chance for Void Slash skillbook to appear in trader stock");
            CreateSliderSetting(listingStandard, "Gun Stinger book", ref settings.gunStingerTradeChance, 
                0f, 15f, "%", "Chance for Gun Stinger skillbook to appear in trader stock");
            CreateSliderSetting(listingStandard, "Heavy Rain book", ref settings.heavyRainTradeChance, 
                0f, 15f, "%", "Chance for Heavy Rain skillbook to appear in trader stock");
            CreateSliderSetting(listingStandard, "Rain Bullet book", ref settings.rainBulletTradeChance, 
                0f, 15f, "%", "Chance for Rain Bullet skillbook to appear in trader stock");
            CreateSliderSetting(listingStandard, "Rapid Slash book", ref settings.rapidSlashTradeChance, 
                0f, 15f, "%", "Chance for Rapid Slash skillbook to appear in trader stock");
            CreateSliderSetting(listingStandard, "Red Hot Night book", ref settings.redHotNightTradeChance, 
                0f, 15f, "%", "Chance for Red Hot Night skillbook to appear in trader stock");
            CreateSliderSetting(listingStandard, "Devil Trigger book", ref settings.devilTriggerTradeChance, 
                0f, 5f, "%", "Chance for Devil Trigger skillbook to appear in trader stock (very rare)");
            CreateSliderSetting(listingStandard, "Sin Devil Trigger book", ref settings.sinDevilTriggerTradeChance, 
                0f, 2f, "%", "Chance for Sin Devil Trigger skillbook to appear in trader stock (extremely rare)");
            
            // === DMC CALLOUTS ===
            DrawSectionHeader(listingStandard, "DMC Callouts & Speech");
            Text.Font = GameFont.Tiny;
            listingStandard.Label("Pawns will say iconic Devil May Cry quotes when using abilities:");
            Text.Font = GameFont.Small;
            listingStandard.Gap(5f);
            
            // Enable/disable callouts
            listingStandard.CheckboxLabeled("Enable callouts", ref settings.calloutsEnabled,
                "Show floating text with Devil May Cry quotes when using abilities");
            
            // Show in message log too
            listingStandard.CheckboxLabeled("Show callouts in message log", ref settings.calloutMessagesEnabled,
                "Also display callout text in the message log (bottom left)");
            
            // Callout frequency
            CreateSliderSetting(listingStandard, "Callout frequency", ref settings.calloutChance, 
                0f, 100f, "%", "How often callouts appear when using abilities");
            
            // Reset button
            listingStandard.Gap(20f);
            if (listingStandard.ButtonText("Reset All to Defaults"))
            {
                ResetToDefaults();
            }
            
            listingStandard.Gap(10f);
            Text.Font = GameFont.Tiny;
            listingStandard.Label("DMC Abilities v1.6+ - All abilities are inspired by Devil May Cry series");
            Text.Font = GameFont.Small;
            
            listingStandard.End();
            Widgets.EndScrollView();
            
            base.DoSettingsWindowContents(inRect);
        }
        
        private void DrawSectionHeader(Listing_Standard listing, string title)
        {
            listing.Gap(15f);
            
            // Draw a separator line
            Rect lineRect = listing.GetRect(1f);
            lineRect.width *= 0.8f;
            Widgets.DrawLineHorizontal(lineRect.x, lineRect.y, lineRect.width);
            
            listing.Gap(8f);
            Text.Font = GameFont.Medium;
            GUI.color = new Color(0.8f, 0.9f, 1.0f); // Light blue tint
            listing.Label(title);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            listing.Gap(8f);
        }
        
        private void CreateAbilityToggle(Listing_Standard listing, string name, ref bool setting, string tooltip)
        {
            listing.CheckboxLabeled($"Enable {name}", ref setting, tooltip);
        }
        
        private void CreateSliderSetting(Listing_Standard listing, string name, ref float setting, 
            float min, float max, string suffix, string tooltip = null)
        {
            listing.Label($"{name}: {setting:F1}{suffix}");
            setting = listing.Slider(setting, min, max);
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(listing.GetRect(0f), tooltip);
            }
            listing.Gap(3f);
        }
        
        private void ResetToDefaults()
        {
            // Main settings
            settings.modEnabled = true;
            settings.disableFriendlyFire = false;
            
            // Ability toggles
            settings.stingerEnabled = true;
            settings.judgementCutEnabled = true;
            settings.driveEnabled = true;
            settings.voidSlashEnabled = true;
            settings.gunStingerEnabled = true;
            settings.heavyRainEnabled = true;
            settings.rainBulletEnabled = true;
            settings.rapidSlashEnabled = true;
            settings.redHotNightEnabled = true;
            
            // Performance
            settings.maxRedHotOrbs = 20;
            
            // Damage multipliers
            settings.stingerDamageMultiplier = 1.2f;
            settings.judgementCutDamageMultiplier = 1.0f;
            settings.driveDamageMultiplier = 1.0f;
            settings.voidSlashDamageMultiplier = 1.0f;
            settings.gunStingerDamageMultiplier = 1.5f;
            settings.heavyRainDamageMultiplier = 1.0f;
            settings.rainBulletDamageMultiplier = 1.0f;
            settings.rapidSlashDamageMultiplier = 1.0f;
            settings.redHotNightDamageMultiplier = 1.0f;
            settings.swordDamageBonus = 10f;
            
            // Trader chances
            settings.stingerTradeChance = 5f;
            settings.judgementCutTradeChance = 3f;
            settings.driveTradeChance = 4f;
            settings.voidSlashTradeChance = 4f;
            settings.gunStingerTradeChance = 4f;
            settings.heavyRainTradeChance = 2f;
            settings.rainBulletTradeChance = 4f;
            settings.rapidSlashTradeChance = 3f;
            settings.redHotNightTradeChance = 2f;
            settings.devilTriggerTradeChance = 1f;
            settings.sinDevilTriggerTradeChance = 0.5f;
            
            // Speech/Callout settings
            settings.calloutsEnabled = true;
            settings.calloutMessagesEnabled = false;
            settings.calloutChance = 75f;
        }

        public override string SettingsCategory()
        {
            return "DMC Abilities";
        }
    }

    public class DMCAbilitiesSettings : ModSettings
    {
        // Main settings
        public bool modEnabled = true;
        public bool disableFriendlyFire = false;
        
        // Ability toggles
        public bool stingerEnabled = true;
        public bool judgementCutEnabled = true;
        public bool driveEnabled = true;
        public bool voidSlashEnabled = true;
        public bool gunStingerEnabled = true;
        public bool heavyRainEnabled = true;
        public bool rainBulletEnabled = true;
        public bool rapidSlashEnabled = true;
        public bool redHotNightEnabled = true;
        public bool devilTriggerEnabled = true;
        public bool sinDevilTriggerEnabled = true;
        
        // Performance settings
        public int maxRedHotOrbs = 20;
        
        // Damage multipliers
        public float stingerDamageMultiplier = 1.2f;
        public float driveDamageMultiplier = 1.0f;
        public float gunStingerDamageMultiplier = 1.5f;
        public float judgementCutDamageMultiplier = 1.0f;
        public float voidSlashDamageMultiplier = 1.0f;
        public float heavyRainDamageMultiplier = 1.0f;
        public float rainBulletDamageMultiplier = 1.0f;
        public float rapidSlashDamageMultiplier = 1.0f;
        public float redHotNightDamageMultiplier = 1.0f;
        public float devilTriggerDamageMultiplier = 1.5f;
        public float sinDevilTriggerDamageMultiplier = 2.0f;
        public float swordDamageBonus = 10f;
        
        // Trader chances for skillbooks
        public float stingerTradeChance = 5f;
        public float judgementCutTradeChance = 3f;
        public float driveTradeChance = 4f;
        public float voidSlashTradeChance = 4f;
        public float gunStingerTradeChance = 4f;
        public float heavyRainTradeChance = 2f;
        public float rainBulletTradeChance = 4f;
        public float rapidSlashTradeChance = 3f;
        public float redHotNightTradeChance = 2f;
        public float devilTriggerTradeChance = 1f; // Very rare
        public float sinDevilTriggerTradeChance = 0.5f; // Extremely rare
        
        // Speech/Callout settings
        public bool calloutsEnabled = true;
        public bool calloutMessagesEnabled = false;
        public float calloutChance = 75f; // Default 75% chance

        public override void ExposeData()
        {
            // Main settings
            Scribe_Values.Look(ref modEnabled, "modEnabled", true);
            Scribe_Values.Look(ref disableFriendlyFire, "disableFriendlyFire", false);
            
            // Ability toggles
            Scribe_Values.Look(ref stingerEnabled, "stingerEnabled", true);
            Scribe_Values.Look(ref judgementCutEnabled, "judgementCutEnabled", true);
            Scribe_Values.Look(ref driveEnabled, "driveEnabled", true);
            Scribe_Values.Look(ref voidSlashEnabled, "voidSlashEnabled", true);
            Scribe_Values.Look(ref gunStingerEnabled, "gunStingerEnabled", true);
            Scribe_Values.Look(ref heavyRainEnabled, "heavyRainEnabled", true);
            Scribe_Values.Look(ref rainBulletEnabled, "rainBulletEnabled", true);
            Scribe_Values.Look(ref rapidSlashEnabled, "rapidSlashEnabled", true);
            Scribe_Values.Look(ref redHotNightEnabled, "redHotNightEnabled", true);
            Scribe_Values.Look(ref devilTriggerEnabled, "devilTriggerEnabled", true);
            Scribe_Values.Look(ref sinDevilTriggerEnabled, "sinDevilTriggerEnabled", true);
            
            // Performance settings
            Scribe_Values.Look(ref maxRedHotOrbs, "maxRedHotOrbs", 20);
            
            // Damage multipliers
            Scribe_Values.Look(ref stingerDamageMultiplier, "stingerDamageMultiplier", 1.2f);
            Scribe_Values.Look(ref driveDamageMultiplier, "driveDamageMultiplier", 1.0f);
            Scribe_Values.Look(ref gunStingerDamageMultiplier, "gunStingerDamageMultiplier", 1.5f);
            Scribe_Values.Look(ref judgementCutDamageMultiplier, "judgementCutDamageMultiplier", 1.0f);
            Scribe_Values.Look(ref voidSlashDamageMultiplier, "voidSlashDamageMultiplier", 1.0f);
            Scribe_Values.Look(ref heavyRainDamageMultiplier, "heavyRainDamageMultiplier", 1.0f);
            Scribe_Values.Look(ref rainBulletDamageMultiplier, "rainBulletDamageMultiplier", 1.0f);
            Scribe_Values.Look(ref rapidSlashDamageMultiplier, "rapidSlashDamageMultiplier", 1.0f);
            Scribe_Values.Look(ref redHotNightDamageMultiplier, "redHotNightDamageMultiplier", 1.0f);
            Scribe_Values.Look(ref devilTriggerDamageMultiplier, "devilTriggerDamageMultiplier", 1.5f);
            Scribe_Values.Look(ref sinDevilTriggerDamageMultiplier, "sinDevilTriggerDamageMultiplier", 2.0f);
            Scribe_Values.Look(ref swordDamageBonus, "swordDamageBonus", 10f);
            
            // Trader chances
            Scribe_Values.Look(ref stingerTradeChance, "stingerTradeChance", 5f);
            Scribe_Values.Look(ref judgementCutTradeChance, "judgementCutTradeChance", 3f);
            Scribe_Values.Look(ref driveTradeChance, "driveTradeChance", 4f);
            Scribe_Values.Look(ref voidSlashTradeChance, "voidSlashTradeChance", 4f);
            Scribe_Values.Look(ref gunStingerTradeChance, "gunStingerTradeChance", 4f);
            Scribe_Values.Look(ref heavyRainTradeChance, "heavyRainTradeChance", 2f);
            Scribe_Values.Look(ref rainBulletTradeChance, "rainBulletTradeChance", 4f);
            Scribe_Values.Look(ref rapidSlashTradeChance, "rapidSlashTradeChance", 3f);
            Scribe_Values.Look(ref redHotNightTradeChance, "redHotNightTradeChance", 2f);
            Scribe_Values.Look(ref devilTriggerTradeChance, "devilTriggerTradeChance", 1f);
            Scribe_Values.Look(ref sinDevilTriggerTradeChance, "sinDevilTriggerTradeChance", 0.5f);
            
            // Speech/Callout settings
            Scribe_Values.Look(ref calloutsEnabled, "calloutsEnabled", true);
            Scribe_Values.Look(ref calloutMessagesEnabled, "calloutMessagesEnabled", false);
            Scribe_Values.Look(ref calloutChance, "calloutChance", 75f);
            
            base.ExposeData();
        }
    }
}