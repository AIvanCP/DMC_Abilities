using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace DMCAbilities
{
    // Definition class for speech categories
    public class DMC_SpeechCategoryDef : Def
    {
        public List<string> phrases = new List<string>();
        public Color color = Color.white;
    }

    // Static utility class for handling DMC callouts
    public static class DMCSpeechUtility
    {
        private static Dictionary<string, DMC_SpeechCategoryDef> speechCache = new Dictionary<string, DMC_SpeechCategoryDef>();
        
        // Initialize speech cache on first use
        static DMCSpeechUtility()
        {
            RefreshSpeechCache();
        }
        
        private static void RefreshSpeechCache()
        {
            speechCache.Clear();
            foreach (var speechDef in DefDatabase<DMC_SpeechCategoryDef>.AllDefs)
            {
                speechCache[speechDef.defName] = speechDef;
            }
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
            if (!DMCAbilitiesMod.settings.calloutsEnabled)
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
            if (!speechCache.TryGetValue(categoryDefName, out DMC_SpeechCategoryDef speechDef))
            {
                Log.Warning($"[DMC Abilities] Speech category '{categoryDefName}' not found");
                return;
            }
            
            if (speechDef.phrases == null || !speechDef.phrases.Any())
            {
                Log.Warning($"[DMC Abilities] Speech category '{categoryDefName}' has no phrases");
                return;
            }
            
            // Pick a random phrase
            string phrase = speechDef.phrases.RandomElement();
            
            // Calculate position slightly above and offset from pawn
            Vector3 position = pawn.Position.ToVector3Shifted() + Vector3.up * 1.5f;
            
            // Add slight random offset so multiple callouts don't overlap
            position += new Vector3(Rand.Range(-0.5f, 0.5f), 0f, Rand.Range(-0.5f, 0.5f));
            
            // Create the floating text
            MoteMaker.ThrowText(
                position,
                pawn.Map,
                phrase,
                speechDef.color,
                3.85f  // Duration - slightly longer for longer phrases
            );
            
            // Optional: Also log to message log if enabled in settings
            if (DMCAbilitiesMod.settings.calloutMessagesEnabled)
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
            if (!DMCAbilitiesMod.settings.calloutsEnabled || pawn?.Map == null)
                return;
                
            Vector3 position = pawn.Position.ToVector3Shifted() + Vector3.up * 1.5f;
            position += new Vector3(Rand.Range(-0.5f, 0.5f), 0f, Rand.Range(-0.5f, 0.5f));
            
            MoteMaker.ThrowText(position, pawn.Map, text, color, duration);
        }
        
        /// <summary>
        /// Get available speech categories for debugging
        /// </summary>
        public static IEnumerable<string> GetAvailableCategories()
        {
            return speechCache.Keys;
        }
        
        /// <summary>
        /// Get phrases from a specific category for debugging
        /// </summary>
        public static List<string> GetPhrasesFor(string categoryDefName)
        {
            if (speechCache.TryGetValue(categoryDefName, out DMC_SpeechCategoryDef speechDef))
            {
                return speechDef.phrases ?? new List<string>();
            }
            return new List<string>();
        }
    }
}