using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace DMCAbilities
{
    /// <summary>
    /// Minimal stub Def class to satisfy RimWorld's XML parser for DMC_SpeechDefs.xml.
    /// This class is NOT used at runtime - all speech phrases are hardcoded in DMCSpeechUtility.
    /// Exists only to prevent "Type DMC_SpeechCategoryDef not found" errors during mod loading.
    /// </summary>
    public class DMC_SpeechCategoryDef : Def
    {
        public List<string> phrases = new List<string>();
        public Color color = Color.white;
    }
}
