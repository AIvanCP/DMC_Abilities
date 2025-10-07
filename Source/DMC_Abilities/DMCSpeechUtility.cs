using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace DMCAbilities
{
    // Internal class to hold speech data
    internal class SpeechCategory
    {
        public List<string> phrases;
        public Color color;

        public SpeechCategory(List<string> phrases, Color color)
        {
            this.phrases = phrases;
            this.color = color;
        }
    }

    // Static utility class for handling DMC callouts with direct MoteMaker calls
    public static class DMCSpeechUtility
    {
        private static Dictionary<string, SpeechCategory> speechData;
        
        // Initialize speech data on first use
        static DMCSpeechUtility()
        {
            InitializeSpeechData();
        }
        
        private static void InitializeSpeechData()
        {
            speechData = new Dictionary<string, SpeechCategory>
            {
                // Devil Trigger Activation Quotes
                ["DMC_DevilTriggerActivation"] = new SpeechCategory(
                    new List<string> { "Don't fuck with me!", "Time to get serious!", "Watch this!", 
                        "This party's getting crazy!", "I need more power!", "Showtime!", 
                        "Now I'm motivated!", "Let's dance!" },
                    new Color(1.0f, 0.3f, 0.3f) // Red
                ),
                
                // Sin Devil Trigger Activation Quotes
                ["DMC_SinDevilTriggerActivation"] = new SpeechCategory(
                    new List<string> { "Your nightmare begins here!", "My turn now!", "Foolishness!", 
                        "No Not Yet!", "This Is Power!", "Power! Give me more power!" },
                    new Color(0.8f, 0.1f, 1.0f) // Purple
                ),
                
                // Combat Success Quotes
                ["DMC_CombatSuccess"] = new SpeechCategory(
                    new List<string> { "Scum!", "Too easy!", "Piece of cake!", "Don't get cocky!", 
                        "Stand Aside!", "Pointless!", "You Wretch!", "Outta My Sight!", "Be Gone!" },
                    new Color(1.0f, 0.8f, 0.0f) // Gold
                ),
                
                // Stinger Activation
                ["DMC_StingerActivation"] = new SpeechCategory(
                    new List<string> { "EEIIYYAAHH!", "EEIIYYDDAAHH!", "HYYAAHH!" },
                    new Color(0.7f, 0.7f, 1.0f) // Light blue
                ),
                
                // Judgement Cut Activation
                ["DMC_JudgementCutActivation"] = new SpeechCategory(
                    new List<string> { "Cut You Down!", "Kneel Before Me!", "You're Finished!", "Don't Move!" },
                    new Color(1.0f, 0.8f, 0.8f) // Light red
                ),
                
                // Rapid Slash Activation
                ["DMC_RapidSlashActivation"] = new SpeechCategory(
                    new List<string> { "Too Slow!", "Clean Cut!", "Go To Hell!" },
                    new Color(0.9f, 1.0f, 0.8f) // Light green
                ),
                
                // Void Slash Activation
                ["DMC_VoidSlashActivation"] = new SpeechCategory(
                    new List<string> { "Pathetic!", "Exhilarating!", "Slice through!" },
                    new Color(0.4f, 0.1f, 0.8f) // Dark purple
                ),
                
                // Gun Stinger Activation
                ["DMC_GunStingerActivation"] = new SpeechCategory(
                    new List<string> { "Hell Yeah!", "Bang bang!", "Blast!", "Eat this!" },
                    new Color(1.0f, 0.6f, 0.0f) // Orange
                ),
                
                // Heavy Rain Activation
                ["DMC_HeavyRainActivation"] = new SpeechCategory(
                    new List<string> { "Watch The Sky!", "Eat This!", "Dodge This!" },
                    new Color(0.6f, 0.8f, 1.0f) // Light blue
                ),
                
                // Rain Bullet Activation
                ["DMC_RainBulletActivation"] = new SpeechCategory(
                    new List<string> { "Rainning Bullet!", "Payday!", "Hell Yeah!" },
                    new Color(0.8f, 0.8f, 0.6f) // Yellow-gray
                ),
                
                // Drive Activation
                ["DMC_DriveActivation"] = new SpeechCategory(
                    new List<string> { "Blast!", "Outta My Sight!", "Drive!", "Go To Hell!", "It's Over!" },
                    new Color(0.0f, 0.8f, 1.0f) // Cyan
                ),
                
                // Red Hot Night Activation
                ["DMC_RedHotNightActivation"] = new SpeechCategory(
                    new List<string> { "Outrun This!", "Red Hot!", "Gettin' hype!", 
                        "I'll Show You What I Got!", "Locked on!" },
                    new Color(1.0f, 0.4f, 0.1f) // Red-orange
                ),
                
                // Taking Damage Quotes
                ["DMC_TakingDamage"] = new SpeechCategory(
                    new List<string> { "Is that all?", "Not even close!", "You'll have to try harder!", 
                        "Tch!", "Not Bad!" },
                    new Color(1.0f, 0.5f, 0.0f) // Orange
                ),
                
                // Low Health Quotes
                ["DMC_LowHealth"] = new SpeechCategory(
                    new List<string> { "I won't lose!", "Not yet!", "I'm Getting Old!", "This isn't over!" },
                    new Color(1.0f, 0.2f, 0.2f) // Bright red
                )
            };
        }
        
        /// <summary>
        /// Displays a random phrase from the specified speech category as floating text above the pawn
        /// </summary>
        /// <param name="pawn">The pawn saying the phrase</param>
        /// <param name="categoryDefName">The speech category to pick from</param>
        /// <param name="chancePercent">Chance (0-100) that the callout will actually trigger</param>
        public static void TryShowCallout(Pawn pawn, string categoryDefName, float chancePercent = 100f)
        {
            // Check if callouts are enabled in mod settings
            if (DMCAbilitiesMod.settings != null && !DMCAbilitiesMod.settings.calloutsEnabled)
                return;
                
            // Random chance check
            if (Rand.Range(0f, 100f) > chancePercent)
                return;
                
            // Safety checks
            if (pawn?.Map == null || !pawn.Position.IsValid)
                return;
                
            try
            {
                ShowCallout(pawn, categoryDefName);
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[DMC Abilities] Failed to show callout for {categoryDefName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Forcibly displays a random phrase from the specified speech category
        /// </summary>
        public static void ShowCallout(Pawn pawn, string categoryDefName)
        {
            if (!speechData.TryGetValue(categoryDefName, out SpeechCategory category))
            {
                Log.Warning($"[DMC Abilities] Speech category '{categoryDefName}' not found");
                return;
            }
            
            if (category.phrases == null || !category.phrases.Any())
            {
                Log.Warning($"[DMC Abilities] Speech category '{categoryDefName}' has no phrases");
                return;
            }
            
            // Pick a random phrase
            string phrase = category.phrases.RandomElement();
            
            // Calculate position slightly above and offset from pawn
            Vector3 position = pawn.Position.ToVector3Shifted() + Vector3.up * 1.5f;
            
            // Add slight random offset so multiple callouts don't overlap
            position += new Vector3(Rand.Range(-0.5f, 0.5f), 0f, Rand.Range(-0.5f, 0.5f));
            
            // Create the floating text as a MoteText so we can set its Scale
            IntVec3 intVec = position.ToIntVec3();
            if (intVec.InBounds(pawn.Map))
            {
                MoteText moteText = (MoteText)ThingMaker.MakeThing(ThingDefOf.Mote_Text);
                moteText.exactPosition = position;
                moteText.SetVelocity(Rand.Range(5, 35), Rand.Range(0.42f, 0.45f));
                moteText.text = phrase;
                moteText.textColor = category.color;
                // Set visual size to 1.5
                moteText.Scale = 1.5f;
                // Keep the same duration/fade timing
                moteText.overrideTimeBeforeStartFadeout = 3.85f;
                GenSpawn.Spawn(moteText, intVec, pawn.Map);
            }
            
            // Optional: Also log to message log if enabled in settings
            if (DMCAbilitiesMod.settings != null && DMCAbilitiesMod.settings.calloutMessagesEnabled)
            {
                Messages.Message(
                    $"{pawn.Name.ToStringShort}: \"{phrase}\"",
                    pawn,
                    MessageTypeDefOf.SilentInput,
                    false  // Don't repeat if same message
                );
            }
        }
        
        /// <summary>
        /// Show callout with custom text and color
        /// </summary>
        public static void ShowCustomCallout(Pawn pawn, string text, Color color, float duration = 3.85f)
        {
            if (DMCAbilitiesMod.settings != null && !DMCAbilitiesMod.settings.calloutsEnabled)
                return;
                
            if (pawn?.Map == null)
                return;
                
            Vector3 position = pawn.Position.ToVector3Shifted() + Vector3.up * 1.5f;
            position += new Vector3(Rand.Range(-0.5f, 0.5f), 0f, Rand.Range(-0.5f, 0.5f));
            
            IntVec3 intVec = position.ToIntVec3();
            if (intVec.InBounds(pawn.Map))
            {
                MoteText moteText = (MoteText)ThingMaker.MakeThing(ThingDefOf.Mote_Text);
                moteText.exactPosition = position;
                moteText.SetVelocity(Rand.Range(5, 35), Rand.Range(0.42f, 0.45f));
                moteText.text = text;
                moteText.textColor = color;
                moteText.Scale = 1.5f;
                moteText.overrideTimeBeforeStartFadeout = duration;
                GenSpawn.Spawn(moteText, intVec, pawn.Map);
            }
        }
        
        /// <summary>
        /// Get available speech categories for debugging
        /// </summary>
        public static IEnumerable<string> GetAvailableCategories()
        {
            return speechData.Keys;
        }
        
        /// <summary>
        /// Get phrases from a specific category for debugging
        /// </summary>
        public static List<string> GetPhrasesFor(string categoryDefName)
        {
            if (speechData.TryGetValue(categoryDefName, out SpeechCategory category))
            {
                return category.phrases ?? new List<string>();
            }
            return new List<string>();
        }
    }
}
