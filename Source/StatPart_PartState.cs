using RimWorld;
using Verse;

namespace PawnExtensions
{
    internal class StatPart_PartState : StatPart
    {
        private SimpleCurve curve = new SimpleCurve { new CurvePoint(0, 0), new CurvePoint(1, 1) };
        private BodyPartTagDef tag;

        public override string ExplanationPart(StatRequest req)
        {
            if (req.Thing is Pawn pawn)
            {
                return "Part efficiency" + $": x{Calculate(pawn).ToStringPercent()}"; // TODO: turn into a translated string
            }

            return null;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.Thing is Pawn pawn)
            {
                val *= Calculate(pawn);
            }
        }

        private float Calculate(Pawn pawn)
        {
            float efficiency = PawnCapacityUtility.CalculateTagEfficiency(pawn.health.hediffSet, tag);
            return curve.Evaluate(efficiency);
        }
    }
}