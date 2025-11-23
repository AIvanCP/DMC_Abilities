using RimWorld;
using Verse;

namespace DMCAbilities
{
    public class CompUseEffect_LearnAbility : CompUseEffect
    {
        private CompProperties_UseEffect_LearnAbility Props => (CompProperties_UseEffect_LearnAbility)props;

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);

            if (usedBy == null)
                return;

            // NEW SYSTEM: Grant ability directly without hediff
            if (!string.IsNullOrEmpty(Props.abilityDefName))
            {
                if (!DMCAbilityUtility.TryUnlockAbility(usedBy, Props.abilityDefName, out string abilityLabel))
                {
                    Messages.Message("DMC_AlreadyKnowsAbility".Translate(usedBy.Name.ToStringShort, abilityLabel), 
                        usedBy, MessageTypeDefOf.RejectInput, false);
                    return;
                }

                // Success message
                Messages.Message("DMC_LearnedAbility".Translate(usedBy.Name.ToStringShort, abilityLabel), 
                    usedBy, MessageTypeDefOf.PositiveEvent, false);

                // Add some XP to intellectual skill
                if (usedBy.skills != null)
                {
                    usedBy.skills.Learn(SkillDefOf.Intellectual, 500f, false);
                }
                return;
            }

            // LEGACY FALLBACK: Old hediff system (kept for compatibility, but not used)
            if (Props.hediffToAdd != null)
            {
                if (usedBy.health.hediffSet.HasHediff(Props.hediffToAdd))
                {
                    Messages.Message("DMC_AlreadyKnowsAbility".Translate(usedBy.Name.ToStringShort, Props.hediffToAdd.label), 
                        usedBy, MessageTypeDefOf.RejectInput, false);
                    return;
                }

                Hediff hediff = HediffMaker.MakeHediff(Props.hediffToAdd, usedBy);
                usedBy.health.AddHediff(hediff);

                Messages.Message("DMC_LearnedAbility".Translate(usedBy.Name.ToStringShort, Props.hediffToAdd.label), 
                    usedBy, MessageTypeDefOf.PositiveEvent, false);

                if (usedBy.skills != null)
                {
                    usedBy.skills.Learn(SkillDefOf.Intellectual, 500f, false);
                }
            }
        }

        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            AcceptanceReport baseResult = base.CanBeUsedBy(p);
            if (!baseResult.Accepted)
                return baseResult;

            // NEW SYSTEM: Check if ability already known
            if (!string.IsNullOrEmpty(Props.abilityDefName))
            {
                if (DMCAbilityUtility.HasAbility(p, Props.abilityDefName, out string abilityLabel))
                {
                    return "DMC_AlreadyKnowsAbility".Translate(p.Name.ToStringShort, abilityLabel);
                }
                return true;
            }

            // LEGACY FALLBACK
            if (Props.hediffToAdd == null)
            {
                return "DMC_InvalidSkillbook".Translate();
            }

            if (p.health.hediffSet.HasHediff(Props.hediffToAdd))
            {
                return "DMC_AlreadyKnowsAbility".Translate(p.Name.ToStringShort, Props.hediffToAdd.label);
            }

            return true;
        }
    }

    public class CompProperties_UseEffect_LearnAbility : CompProperties_UseEffect
    {
        public HediffDef hediffToAdd; // Legacy - kept for compatibility
        public string abilityDefName; // NEW - direct ability unlock

        public CompProperties_UseEffect_LearnAbility()
        {
            compClass = typeof(CompUseEffect_LearnAbility);
        }
    }
}