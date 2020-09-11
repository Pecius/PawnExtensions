using System.Collections.Generic;
using Verse;

namespace PawnExtensions
{
    public enum PartStateMode : byte
    {
        Undefined,
        Adjust,
        AddRemove
    }

    public class HediffGiver_PartState : HediffGiver
    {
        private List<PartStateRule> rules;

        public override IEnumerable<string> ConfigErrors()
        {
            if (rules != null)
            {
                foreach (PartStateRule rule in rules)
                {
                    foreach (string error in rule.ConfigErrors())
                        yield return error;
                }
            }
            else
                yield return "rules is null";
        }

        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            foreach (PartStateRule rule in rules)
            {
                Pawn_HealthTracker pawnHealth = pawn.health;

                float partEfficiency = PawnCapacityUtility.CalculateTagEfficiency(pawnHealth.hediffSet, rule.tag);
                float severity = rule.curve.Evaluate(partEfficiency);

                Hediff hediff = pawnHealth.hediffSet.GetFirstHediffOfDef(rule.hediff);
                if (hediff == null)
                {
                    if (severity == 0f)
                        continue;

                    hediff = HediffMaker.MakeHediff(rule.hediff, pawn, null);
                    pawnHealth.AddHediff(hediff);
                }

                if (rule.mode != PartStateMode.AddRemove || severity == 0f)
                    hediff.Severity = severity;
            }
        }

        private class PartStateRule : Editable
        {
            public SimpleCurve curve = new SimpleCurve { new CurvePoint(0, 1), new CurvePoint(1, 0) };
            public HediffDef hediff;
            public PartStateMode mode = PartStateMode.Adjust;
            public BodyPartTagDef tag;
            public override IEnumerable<string> ConfigErrors()
            {
                if (tag == null)
                    yield return "tag is null";

                if (hediff == null)
                    yield return "hediff is null";
            }
        }
    }
}