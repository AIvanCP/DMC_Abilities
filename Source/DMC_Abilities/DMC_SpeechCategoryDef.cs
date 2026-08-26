using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace DMCAbilities
{
    /// <summary>
    /// One callout category: the lines a pawn can shout, and the colour they appear in.
    ///
    /// This used to be a stub that nothing read, because the XML wrote the node as
    /// &lt;DMC_SpeechCategoryDef&gt; while the real type is DMCAbilities.DMC_SpeechCategoryDef.
    /// RimWorld resolves def type names through Assembly.GetType, which needs the namespace
    /// for anything outside its own built-in list, so every category was skipped with a
    /// "not a Def type" error. Callouts kept working only because DMCSpeechUtility also
    /// hardcodes the same phrases.
    ///
    /// Both halves are fixed now: the XML is namespace-qualified, and DMCSpeechUtility
    /// treats its hardcoded table as a baseline that any loaded def overrides. Editing the
    /// XML changes what pawns say, with no recompile.
    /// </summary>
    public class DMC_SpeechCategoryDef : Def
    {
        public List<string> phrases = new List<string>();
        public Color color = Color.white;
    }
}
