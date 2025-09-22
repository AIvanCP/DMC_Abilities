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

            if (usedBy == null || Props.hediffToAdd == null)
                return;

            // Check if pawn already has this hediff
            if (usedBy.health.hediffSet.HasHediff(Props.hediffToAdd))
            {
                Messages.Message("DMC_AlreadyKnowsAbility".Translate(usedBy.Name.ToStringShort, Props.hediffToAdd.label), 
                    usedBy, MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Add the hediff that grants the ability
            Hediff hediff = HediffMaker.MakeHediff(Props.hediffToAdd, usedBy);
            usedBy.health.AddHediff(hediff);

            // Success message
            Messages.Message("DMC_LearnedAbility".Translate(usedBy.Name.ToStringShort, Props.hediffToAdd.label), 
                usedBy, MessageTypeDefOf.PositiveEvent, false);

            // Add some XP to intellectual skill
            if (usedBy.skills != null)
            {
                usedBy.skills.Learn(SkillDefOf.Intellectual, 500f, false);
            }
        }

        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            AcceptanceReport baseResult = base.CanBeUsedBy(p);
            if (!baseResult.Accepted)
                return baseResult;

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
        public HediffDef hediffToAdd;

        public CompProperties_UseEffect_LearnAbility()
        {
            compClass = typeof(CompUseEffect_LearnAbility);
        }
    }
}