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
            
            listingStandard.Gap();
            listingStandard.Label("Balance Settings:");
            
            listingStandard.Label($"Stinger damage multiplier: {settings.stingerDamageMultiplier:F1}x");
            settings.stingerDamageMultiplier = listingStandard.Slider(settings.stingerDamageMultiplier, 0.5f, 3.0f);
            
            listingStandard.Label($"Sword damage bonus: {settings.swordDamageBonus:F0}%");
            settings.swordDamageBonus = listingStandard.Slider(settings.swordDamageBonus, 0f, 50f);
            
            listingStandard.Gap();
            listingStandard.Label("Trader Settings:");
            
            listingStandard.Label($"Stinger skillbook trade chance: {settings.stingerTradeChance:F1}%");
            settings.stingerTradeChance = listingStandard.Slider(settings.stingerTradeChance, 0f, 20f);
            
            listingStandard.Label($"Judgement Cut skillbook trade chance: {settings.judgementCutTradeChance:F1}%");
            settings.judgementCutTradeChance = listingStandard.Slider(settings.judgementCutTradeChance, 0f, 15f);
            
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
        public float stingerDamageMultiplier = 1.2f;
        public float swordDamageBonus = 10f;
        public float stingerTradeChance = 5f;
        public float judgementCutTradeChance = 3f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref modEnabled, "modEnabled", true);
            Scribe_Values.Look(ref stingerEnabled, "stingerEnabled", true);
            Scribe_Values.Look(ref judgementCutEnabled, "judgementCutEnabled", true);
            Scribe_Values.Look(ref stingerDamageMultiplier, "stingerDamageMultiplier", 1.2f);
            Scribe_Values.Look(ref swordDamageBonus, "swordDamageBonus", 10f);
            Scribe_Values.Look(ref stingerTradeChance, "stingerTradeChance", 5f);
            Scribe_Values.Look(ref judgementCutTradeChance, "judgementCutTradeChance", 3f);
            base.ExposeData();
        }
    }
}