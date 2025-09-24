using RimWorld;
using UnityEngine;
using Verse;

namespace DMCAbilities
{
    public class DMCAbilitiesMod : Mod
    {
        public static DMCAbilitiesSettings settings;

        public DMCAbilitiesMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<DMCAbilitiesSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            
            listingStandard.Label("DMC Abilities Settings");
            listingStandard.Gap();
            
            listingStandard.CheckboxLabeled("Enable mod (requires restart)", ref settings.modEnabled, 
                "Toggle the entire mod on/off. Requires game restart to take effect.");
            
            listingStandard.Gap();
            listingStandard.Label("Ability Settings:");
            
            listingStandard.CheckboxLabeled("Enable Stinger ability", ref settings.stingerEnabled, 
                "Enable or disable the Stinger dash attack ability.");
            
            listingStandard.CheckboxLabeled("Enable Judgement Cut ability", ref settings.judgementCutEnabled,
                "Enable or disable the Judgement Cut ranged slash ability.");
            
            listingStandard.CheckboxLabeled("Enable Drive ability", ref settings.driveEnabled,
                "Enable or disable the Drive projectile slash ability.");
            
            listingStandard.CheckboxLabeled("Enable Void Slash ability", ref settings.voidSlashEnabled,
                "Enable or disable the Void Slash debuffing area ability.");
            
            listingStandard.CheckboxLabeled("Enable Gun Stinger ability", ref settings.gunStingerEnabled,
                "Enable or disable the Gun Stinger shotgun dash ability.");
            
            listingStandard.CheckboxLabeled("Enable Heavy Rain ability", ref settings.heavyRainEnabled,
                "Enable or disable the Heavy Rain spectral sword storm ability.");
            
            listingStandard.CheckboxLabeled("Enable Rain Bullet ability", ref settings.rainBulletEnabled,
                "Enable or disable the Rain Bullet aerial shooting ability.");
            
            listingStandard.Gap();
            listingStandard.Label("Balance Settings:");
            
            listingStandard.Label($"Stinger damage multiplier: {settings.stingerDamageMultiplier:F1}x");
            settings.stingerDamageMultiplier = listingStandard.Slider(settings.stingerDamageMultiplier, 0.5f, 3.0f);
            
            listingStandard.Label($"Drive damage multiplier: {settings.driveDamageMultiplier:F1}x");
            settings.driveDamageMultiplier = listingStandard.Slider(settings.driveDamageMultiplier, 0.5f, 3.0f);
            
            listingStandard.Label($"Gun Stinger damage multiplier: {settings.gunStingerDamageMultiplier:F1}x");
            settings.gunStingerDamageMultiplier = listingStandard.Slider(settings.gunStingerDamageMultiplier, 0.5f, 3.0f);
            
            listingStandard.Label($"Sword damage bonus: {settings.swordDamageBonus:F0}%");
            settings.swordDamageBonus = listingStandard.Slider(settings.swordDamageBonus, 0f, 50f);
            
            listingStandard.Gap();
            listingStandard.Label("Trader Settings:");
            
            listingStandard.Label($"Stinger skillbook trade chance: {settings.stingerTradeChance:F1}%");
            settings.stingerTradeChance = listingStandard.Slider(settings.stingerTradeChance, 0f, 20f);
            
            listingStandard.Label($"Judgement Cut skillbook trade chance: {settings.judgementCutTradeChance:F1}%");
            settings.judgementCutTradeChance = listingStandard.Slider(settings.judgementCutTradeChance, 0f, 15f);
            
            listingStandard.Label($"Void Slash skillbook trade chance: {settings.voidSlashTradeChance:F1}%");
            settings.voidSlashTradeChance = listingStandard.Slider(settings.voidSlashTradeChance, 0f, 15f);
            
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "DMC Abilities";
        }
    }

    public class DMCAbilitiesSettings : ModSettings
    {
        public bool modEnabled = true;
        public bool stingerEnabled = true;
        public bool judgementCutEnabled = true;
        public bool driveEnabled = true;
        public bool voidSlashEnabled = true;
        public bool gunStingerEnabled = true;
        public bool heavyRainEnabled = true;
        public bool rainBulletEnabled = true;
        public float stingerDamageMultiplier = 1.2f;
        public float driveDamageMultiplier = 1.0f;
        public float gunStingerDamageMultiplier = 1.5f;
        public float swordDamageBonus = 10f;
        public float stingerTradeChance = 5f;
        public float judgementCutTradeChance = 3f;
        public float driveTradeChance = 4f;
        public float voidSlashTradeChance = 4f;
        public float gunStingerTradeChance = 4f;
        public float heavyRainTradeChance = 2f;
        public float rainBulletTradeChance = 4f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref modEnabled, "modEnabled", true);
            Scribe_Values.Look(ref stingerEnabled, "stingerEnabled", true);
            Scribe_Values.Look(ref judgementCutEnabled, "judgementCutEnabled", true);
            Scribe_Values.Look(ref driveEnabled, "driveEnabled", true);
            Scribe_Values.Look(ref voidSlashEnabled, "voidSlashEnabled", true);
            Scribe_Values.Look(ref gunStingerEnabled, "gunStingerEnabled", true);
            Scribe_Values.Look(ref heavyRainEnabled, "heavyRainEnabled", true);
            Scribe_Values.Look(ref rainBulletEnabled, "rainBulletEnabled", true);
            Scribe_Values.Look(ref stingerDamageMultiplier, "stingerDamageMultiplier", 1.2f);
            Scribe_Values.Look(ref driveDamageMultiplier, "driveDamageMultiplier", 1.0f);
            Scribe_Values.Look(ref gunStingerDamageMultiplier, "gunStingerDamageMultiplier", 1.5f);
            Scribe_Values.Look(ref swordDamageBonus, "swordDamageBonus", 10f);
            Scribe_Values.Look(ref stingerTradeChance, "stingerTradeChance", 5f);
            Scribe_Values.Look(ref judgementCutTradeChance, "judgementCutTradeChance", 3f);
            Scribe_Values.Look(ref driveTradeChance, "driveTradeChance", 4f);
            Scribe_Values.Look(ref voidSlashTradeChance, "voidSlashTradeChance", 4f);
            Scribe_Values.Look(ref gunStingerTradeChance, "gunStingerTradeChance", 4f);
            Scribe_Values.Look(ref heavyRainTradeChance, "heavyRainTradeChance", 2f);
            Scribe_Values.Look(ref rainBulletTradeChance, "rainBulletTradeChance", 4f);
            base.ExposeData();
        }
    }
}