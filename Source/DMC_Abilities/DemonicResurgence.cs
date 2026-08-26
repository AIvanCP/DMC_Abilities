using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DMCAbilities
{
    /// <summary>
    /// Half-demons do not always stay down.
    ///
    /// When something would kill a pawn carrying the demonic blood gene, this rolls once.
    /// On a hit the death is cancelled outright and the demonic half takes over: the pawn
    /// stays standing, ruined, and transforms on the spot.
    ///
    /// HOW THE DEATH IS ACTUALLY CANCELLED
    ///
    /// Not by healing the wounds. Pawn_HealthTracker.ShouldBeDead() opens with:
    ///
    ///     if (hediffSet.HasPreventsDeath) return false;
    ///
    /// which is the same lever the vanilla Deathless gene pulls. Adding a hediff with
    /// preventsDeath true therefore keeps the pawn alive without touching a single injury -
    /// so they get up still torn apart, which is the whole image. When the hediff expires
    /// the wounds are still there, and if they have not been treated by then the pawn dies
    /// after all.
    ///
    /// This lives in DMC_Abilities rather than in the Half-Demon mod because it needs an
    /// assembly, Harmony, the transformation abilities and the callout system - all of
    /// which are already here. Half-Demon stays pure data. Neither mod hard-depends on the
    /// other: the gene is looked up by name and simply never matches if it is absent.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class DemonicResurgence
    {
        /// <summary>Gene that makes a pawn eligible. Owned by the Half-Demon mod.</summary>
        private const string DemonicBloodGeneName = "DMC_DemonicBlood";

        private static GeneDef demonicBloodGene;
        private static bool geneResolved;

        static DemonicResurgence()
        {
            try
            {
                Harmony harmony = new Harmony("dmcabilities.resurgence");
                harmony.Patch(
                    AccessTools.Method(typeof(Pawn), nameof(Pawn.Kill),
                        new[] { typeof(DamageInfo?), typeof(Hediff) }),
                    prefix: new HarmonyMethod(typeof(DemonicResurgence), nameof(Kill_Prefix)));
            }
            catch (Exception e)
            {
                Log.Error("[DMC Abilities] Could not hook demonic resurgence: " + e);
            }
        }

        /// <summary>
        /// Resolved late and once. GeneDefs are not in the database while this class is
        /// first touched, and the gene belongs to a different mod that may not be present.
        /// </summary>
        private static GeneDef Gene
        {
            get
            {
                if (!geneResolved)
                {
                    geneResolved = true;
                    demonicBloodGene = DefDatabase<GeneDef>.GetNamedSilentFail(DemonicBloodGeneName);
                }
                return demonicBloodGene;
            }
        }

        private static bool Eligible(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.genes == null)
            {
                return false;
            }
            if (!DMCAbilitiesMod.settings.resurgenceEnabled)
            {
                return false;
            }

            GeneDef gene = Gene;
            if (gene == null || !pawn.genes.HasActiveGene(gene))
            {
                return false;
            }

            // Already burning through one, or still worn out from the last one.
            if (pawn.health.hediffSet.HasHediff(DMC_HediffDefOf.DMC_DemonicResurgence)
                || pawn.health.hediffSet.HasHediff(DMC_HediffDefOf.DMC_ResurgenceExhaustion))
            {
                return false;
            }

            // A destroyed brain is where even this stops. Same line vanilla's Deathless
            // gene draws, and without it a decapitated pawn would keep standing up.
            BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
            if (brain == null || pawn.health.hediffSet.PartIsMissing(brain))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returning false here skips Pawn.Kill entirely, which is only safe because the
        /// resurgence hediff goes on first - otherwise the next health tick would call
        /// Kill again and the pawn would die a moment later anyway.
        /// </summary>
        private static bool Kill_Prefix(Pawn __instance, DamageInfo? dinfo)
        {
            try
            {
                if (!Eligible(__instance))
                {
                    return true;
                }
                if (!Rand.Chance(DMCAbilitiesMod.settings.resurgenceChance))
                {
                    return true;
                }

                Trigger(__instance, dinfo);
                return false;
            }
            catch (Exception e)
            {
                // Never let this stop a death from resolving. A pawn that fails to die is
                // a far worse bug than one that fails to come back.
                Log.Error("[DMC Abilities] Demonic resurgence failed, allowing death: " + e);
                return true;
            }
        }

        private static void Trigger(Pawn pawn, DamageInfo? dinfo)
        {
            pawn.health.AddHediff(DMC_HediffDefOf.DMC_DemonicResurgence);
            pawn.health.AddHediff(DMC_HediffDefOf.DMC_ResurgenceExhaustion);

            // Stop the bleeding that was about to finish the job. Wounds are left in
            // place - only the bleed rate is tended away, so the pawn is still a wreck
            // but is not counting down any more.
            TendBleeding(pawn);

            // Back on their feet. Downed state is recalculated from capacities, and the
            // resurgence hediff caps those low rather than restoring them, so the pawn
            // moves at a crawl.
            if (pawn.Downed)
            {
                pawn.health.forceDowned = false;
                pawn.health.Notify_HediffChanged(null);
            }

            CastTransformation(pawn);

            DMCSpeechUtility.TryShowCallout(pawn, "DMC_SinDevilTriggerActivation", 100f);

            if (pawn.Map != null)
            {
                FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect, 3f);
                for (int i = 0; i < 12; i++)
                {
                    IntVec3 c = pawn.Position + GenRadial.RadialPattern[i + 1];
                    if (c.InBounds(pawn.Map))
                    {
                        FleckMaker.ThrowDustPuff(c.ToVector3Shifted(), pawn.Map, 1.4f);
                    }
                }
                SoundDefOf.PsycastPsychicPulse?.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            }

            if (pawn.Faction == Faction.OfPlayer)
            {
                Messages.Message(
                    "DMC_ResurgenceMessage".Translate(pawn.LabelShortCap),
                    pawn, MessageTypeDefOf.PositiveEvent, historical: false);
            }
        }

        /// <summary>Tends every bleeding wound at a fixed high quality, nothing else.</summary>
        private static void TendBleeding(Pawn pawn)
        {
            foreach (Hediff h in pawn.health.hediffSet.hediffs.ToList())
            {
                if (h.Bleeding && h.TryGetComp<HediffComp_TendDuration>() != null)
                {
                    h.Tended(1f, 1f);
                }
            }
        }

        /// <summary>
        /// Sin Devil Trigger if the pawn has it available, otherwise Devil Trigger.
        /// Cooldowns are ignored - this is the demon taking over, not the pawn choosing.
        /// </summary>
        private static void CastTransformation(Pawn pawn)
        {
            if (pawn.abilities == null)
            {
                return;
            }

            Ability sdt = pawn.abilities.abilities.FirstOrDefault(
                a => a.def == DMC_AbilityDefOf.DMC_SinDevilTrigger);
            Ability dt = pawn.abilities.abilities.FirstOrDefault(
                a => a.def == DMC_AbilityDefOf.DMC_DevilTrigger);

            Ability chosen = sdt ?? dt;
            if (chosen == null)
            {
                return;   // no transformation known; they still survive
            }

            HediffDef form = chosen == sdt
                ? DMC_HediffDefOf.DMC_SinDevilTrigger
                : DMC_HediffDefOf.DMC_DevilTrigger;

            if (!pawn.health.hediffSet.HasHediff(form))
            {
                pawn.health.AddHediff(form);
            }
            chosen.StartCooldown(chosen.def.cooldownTicksRange.TrueMax);
        }
    }
}
